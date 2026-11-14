using AAB.EBA.Blockchains.Bitcoin;
using AAB.EBA.CLI.Config;

using Microsoft.Extensions.Logging.Abstractions;

using System.Net.Http;

namespace AAB.EBA.Tests.TestKit.BitcoinCore;

public static class TestBitcoinAgent
{
    public static BitcoinChainAgent Create(HttpMessageHandler node) => new(
        new HttpClient(node, disposeHandler: false) { BaseAddress = new BitcoinTraverseOptions().ClientUri },
        NullLogger<BitcoinChainAgent>.Instance);
}
