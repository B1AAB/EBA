using EBA.Graph.Bitcoin.Strategies;

namespace EBA.Graph.Db.Neo4jDb;

public class BitcoinNeo4jDb(Options options, ILogger<Neo4jDb<BitcoinGraph>> logger) :
    Neo4jDb<BitcoinGraph>(
        options,
        logger,
        new BitcoinStrategyFactory(options))
{ }