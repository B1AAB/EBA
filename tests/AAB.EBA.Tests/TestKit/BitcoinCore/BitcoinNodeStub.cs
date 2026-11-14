using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AAB.EBA.Tests.TestKit.BitcoinCore;

public abstract class BitcoinNodeStub : HttpMessageHandler
{
    private readonly string _chainInfo;
    private readonly ConcurrentQueue<BitcoinCoreRequest> _requests = new();
    private readonly ConcurrentQueue<string> _unexpectedRequests = new();
    private readonly List<RequestRule> _rules = [];
    private readonly object _rulesLock = new();
    private static readonly Regex HashPattern = new(@"/blockhashbyheight/(\d+)\.hex$", RegexOptions.CultureInvariant);
    private static readonly Regex BlockPattern = new(@"/block/([0-9a-fA-F]{64})\.json$", RegexOptions.CultureInvariant);

    protected BitcoinNodeStub(string chainInfo) => _chainInfo = chainInfo;

    public IReadOnlyList<BitcoinCoreRequest> Requests => [.. _requests];
    public IReadOnlyList<string> UnexpectedRequests => [.. _unexpectedRequests];
    public int CountRequests(BitcoinCoreEndpoint endpoint, long? height) => _requests.Count(x => x.Endpoint == endpoint && x.Height == height);
    public IReadOnlyList<BitcoinCoreRequest> GetRequests(BitcoinCoreEndpoint endpoint) => [.. _requests.Where(x => x.Endpoint == endpoint)];

    public RequestRule On(BitcoinCoreEndpoint endpoint, long? fromHeight = null)
    {
        var rule = new RequestRule(endpoint, fromHeight);
        lock (_rulesLock)
            _rules.Add(rule);
        return rule;
    }

    protected abstract string? GetBlockHashCore(long height);
    protected abstract long? ResolveHeight(string hash);
    protected abstract string? GetBlock(long height);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        BitcoinCoreRequest? parsed = null;
        if (path.EndsWith("/chaininfo.json", StringComparison.Ordinal))
            parsed = new(BitcoinCoreEndpoint.ChainInfo, null);
        else if (HashPattern.Match(path) is { Success: true } hashMatch
            && long.TryParse(hashMatch.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var height))
            parsed = new(BitcoinCoreEndpoint.BlockHashByHeight, height);
        else if (BlockPattern.Match(path) is { Success: true } blockMatch)
            parsed = new(BitcoinCoreEndpoint.Block, ResolveHeight(blockMatch.Groups[1].Value));

        if (parsed is not null)
        {
            _requests.Enqueue(parsed);
            RequestRule? rule;
            lock (_rulesLock)
                rule = _rules.Find(x => x.Matches(parsed.Endpoint, parsed.Height));
            if (rule is not null)
                await rule.ApplyAsync(parsed, cancellationToken);
        }

        var content = parsed?.Endpoint switch
        {
            BitcoinCoreEndpoint.ChainInfo => _chainInfo,
            BitcoinCoreEndpoint.BlockHashByHeight when parsed.Height is long h => GetBlockHashCore(h),
            BitcoinCoreEndpoint.Block when parsed.Height is long h => GetBlock(h),
            _ => null
        };
        if (content is null)
            _unexpectedRequests.Enqueue(path);
        return content is null
            ? new HttpResponseMessage(HttpStatusCode.NotFound)
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) };
    }
}
