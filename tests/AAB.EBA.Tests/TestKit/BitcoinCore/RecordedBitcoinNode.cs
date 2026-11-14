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

public sealed class RecordedBitcoinNode : BitcoinNodeStub
{
    private RecordedBitcoinNode() : base(MockData.ChainInfo()) { }

    public static RecordedBitcoinNode FromMockData() => new();

    protected override string? GetBlockHashCore(long height) => MockData.HeightToHash.TryGetValue(height, out var hash) ? hash : null;

    protected override long? ResolveHeight(string hash)
    {
        var match = MockData.HeightToHash.FirstOrDefault(x => x.Value == hash);
        return match.Value is null ? null : match.Key;
    }

    protected override string? GetBlock(long height) => MockData.HeightToHash.ContainsKey(height) ? MockData.BlockJson(height) : null;
}
