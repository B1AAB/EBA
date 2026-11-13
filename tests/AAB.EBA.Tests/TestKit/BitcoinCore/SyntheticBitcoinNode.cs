using AAB.EBA.Blockchains.Bitcoin;
using AAB.EBA.TestDataGenerator;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AAB.EBA.Tests.TestKit.BitcoinCore;

/// <summary>
/// An in-memory stand-in for the REST interface of a Bitcoin Core node,
/// to be used as the primary handler of the <see cref="HttpClient"/> that
/// <see cref="BitcoinChainAgent"/> uses.
///
/// It serves a chain of any length by re-issuing a single template block
/// (taken from the mock data) at every requested height. Each served block
/// has its own height and a hash derived from that height, so every block
/// is a distinct block for the traverse, while their contents (transactions,
/// scripts, etc.) are identical. This makes it possible to traverse many
/// blocks without having mock data for each of them.
///
/// Every request is logged (see <see cref="Requests"/>), and how requests are
/// answered can be changed per endpoint and height with <see cref="On"/>
/// (e.g., to fail, stall, or delay a request), which lets tests drive a
/// client to a known point.
/// </summary>
public sealed partial class SyntheticBitcoinNode : HttpMessageHandler
{
    public const long DefaultTemplateBlockHeight = 71036;

    private readonly string _chainInfo;
    private readonly JsonObject _templateBlock;

    private readonly ConcurrentQueue<BitcoinCoreRequest> _requests = new();
    private readonly List<RequestRule> _rules = [];
    private readonly object _rulesLock = new();

    public SyntheticBitcoinNode(string chainInfo, string templateBlock)
    {
        _chainInfo = chainInfo;
        _templateBlock = JsonNode.Parse(templateBlock)?.AsObject()
            ?? throw new ArgumentException("Invalid template block.", nameof(templateBlock));
    }

    /// <summary>
    /// The requests received so far, in the order of their arrival.
    /// </summary>
    public IReadOnlyList<BitcoinCoreRequest> Requests => [.. _requests];

    /// <summary>
    /// Creates a node that uses the mock chain info and the mock
    /// block at <paramref name="templateBlockHeight"/> as the template.
    /// </summary>
    public static SyntheticBitcoinNode FromMockData(
        long templateBlockHeight = DefaultTemplateBlockHeight)
    {
        var dir = ClientFixture.BitcoinCoreResourcesDir;
        var heightToHash = JsonSerializer.Deserialize<Dictionary<long, string>>(
            File.ReadAllText(Path.Combine(dir, Files.HeightToHashMapFilename)))
            ?? throw new InvalidDataException("Invalid height-to-hash mapping.");

        return new SyntheticBitcoinNode(
            File.ReadAllText(Path.Combine(dir, Files.ChainInfo)),
            File.ReadAllText(Path.Combine(dir, Files.GetMockBlockFilename(heightToHash[templateBlockHeight]))));
    }

    /// <summary>
    /// The hash this node assigns to the block at the given height.
    /// </summary>
    public static string GetBlockHash(long height)
    {
        return height.ToString("x64", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The number of requests received for the given endpoint and height.
    /// </summary>
    public int CountRequests(BitcoinCoreEndpoint endpoint, long? height)
    {
        return _requests.Count(x => x.Endpoint == endpoint && x.Height == height);
    }

    /// <summary>
    /// The requests received for the given endpoint, in the order of their arrival.
    /// </summary>
    public IReadOnlyList<BitcoinCoreRequest> GetRequests(BitcoinCoreEndpoint endpoint)
    {
        return [.. _requests.Where(x => x.Endpoint == endpoint)];
    }

    /// <summary>
    /// Adds a rule for the requests to <paramref name="endpoint"/> for blocks at
    /// or above <paramref name="fromHeight"/> (all the requests to that endpoint
    /// when null). When several rules match a request, the first added applies.
    /// </summary>
    public RequestRule On(BitcoinCoreEndpoint endpoint, long? fromHeight = null)
    {
        var rule = new RequestRule(endpoint, fromHeight);
        lock (_rulesLock)
            _rules.Add(rule);
        return rule;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        if (!TryParse(path, out var parsed))
            return new HttpResponseMessage(HttpStatusCode.NotFound);

        _requests.Enqueue(parsed);

        var rule = FindRule(parsed);
        if (rule is not null)
            await rule.ApplyAsync(parsed, cancellationToken);

        return parsed.Endpoint switch
        {
            BitcoinCoreEndpoint.ChainInfo => Ok(_chainInfo),
            BitcoinCoreEndpoint.BlockHashByHeight => Ok(GetBlockHash(parsed.Height!.Value)),
            BitcoinCoreEndpoint.Block => Ok(GetBlock(parsed.Height!.Value)),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        };
    }

    private RequestRule? FindRule(BitcoinCoreRequest request)
    {
        lock (_rulesLock)
            return _rules.Find(x => x.Matches(request.Endpoint, request.Height));
    }

    private static bool TryParse(string path, out BitcoinCoreRequest request)
    {
        if (path.EndsWith("/chaininfo.json", StringComparison.Ordinal))
        {
            request = new BitcoinCoreRequest(BitcoinCoreEndpoint.ChainInfo, null);
            return true;
        }

        var match = BlockHashByHeightPattern().Match(path);
        if (match.Success)
        {
            request = new BitcoinCoreRequest(
                BitcoinCoreEndpoint.BlockHashByHeight,
                long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
            return true;
        }

        match = BlockByHashPattern().Match(path);
        if (match.Success)
        {
            request = new BitcoinCoreRequest(
                BitcoinCoreEndpoint.Block,
                long.Parse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            return true;
        }

        request = null!;
        return false;
    }

    private string GetBlock(long height)
    {
        var block = _templateBlock.DeepClone().AsObject();
        block["height"] = height;
        block["hash"] = GetBlockHash(height);
        block["previousblockhash"] = GetBlockHash(height - 1);
        block["nextblockhash"] = GetBlockHash(height + 1);
        return block.ToJsonString();
    }

    private static HttpResponseMessage Ok(string content)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content)
        };
    }

    [GeneratedRegex(@"/blockhashbyheight/(\d+)\.hex$")]
    private static partial Regex BlockHashByHeightPattern();

    [GeneratedRegex(@"/block/([0-9a-fA-F]{64})\.json$")]
    private static partial Regex BlockByHashPattern();
}
