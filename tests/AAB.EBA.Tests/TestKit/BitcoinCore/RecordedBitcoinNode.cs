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
using System.Threading;
using System.Threading.Tasks;

namespace AAB.EBA.Tests.TestKit.BitcoinCore;

public sealed class RecordedBitcoinNode : HttpMessageHandler
{
    private readonly string _directory;
    private readonly Dictionary<long, string> _hashes;
    private readonly ConcurrentQueue<BitcoinCoreRequest> _requests = new();
    private readonly ConcurrentQueue<string> _unexpectedRequests = new();

    private RecordedBitcoinNode(string directory)
    {
        _directory = directory;
        _hashes = JsonSerializer.Deserialize<Dictionary<long, string>>(
            File.ReadAllText(Path.Combine(directory, Files.HeightToHashMapFilename)))
            ?? throw new InvalidDataException("Invalid height-to-hash mapping.");
    }

    public IReadOnlyList<BitcoinCoreRequest> Requests => [.. _requests];
    public IReadOnlyList<string> UnexpectedRequests => [.. _unexpectedRequests];

    public static RecordedBitcoinNode FromMockData() => new(ClientFixture.BitcoinCoreResourcesDir);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        string? content = null;
        BitcoinCoreRequest? logged = null;
        if (path == "/rest/chaininfo.json")
        {
            content = File.ReadAllText(Path.Combine(_directory, Files.ChainInfo));
            logged = new(BitcoinCoreEndpoint.ChainInfo, null);
        }
        else if (path.StartsWith("/rest/blockhashbyheight/", StringComparison.Ordinal)
            && path.EndsWith(".hex", StringComparison.Ordinal)
            && long.TryParse(path[24..^4], NumberStyles.None, CultureInfo.InvariantCulture, out var height))
        {
            logged = new(BitcoinCoreEndpoint.BlockHashByHeight, height);
            _hashes.TryGetValue(height, out content);
        }
        else if (path.StartsWith("/rest/block/", StringComparison.Ordinal)
            && path.EndsWith(".json", StringComparison.Ordinal))
        {
            var hash = path[12..^5];
            var match = _hashes.FirstOrDefault(x => x.Value == hash);
            logged = new(BitcoinCoreEndpoint.Block, match.Value is null ? null : match.Key);
            if (match.Value is not null)
                content = File.ReadAllText(Path.Combine(_directory, Files.GetMockBlockFilename(hash)));
        }

        if (logged is not null)
            _requests.Enqueue(logged);
        if (content is null)
            _unexpectedRequests.Enqueue(path);

        return Task.FromResult(content is null
            ? new HttpResponseMessage(HttpStatusCode.NotFound)
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) });
    }
}
