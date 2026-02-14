using EBA.Graph.Bitcoin.Strategies;

namespace EBA.Graph.Db.Neo4jDb;

public class BitcoinNeo4jDb(Options options) :
    Neo4jDb<BitcoinGraph>(
        options,
        new BitcoinStrategyFactory(options))
{ }