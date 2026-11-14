using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.CLI.Config;
using AAB.EBA.Graph.Db.Neo4jDb;

using Microsoft.Extensions.Logging.Abstractions;

namespace AAB.EBA.Tests.TestKit.Graph;

public static class TestGraphDb
{
    public static BitcoinNeo4jDb Create(Options options) => new(options, NullLogger<Neo4jDb<BitcoinGraph>>.Instance);
}
