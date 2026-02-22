using EBA.Graph.Db.Neo4jDb;

namespace EBA.Graph.Bitcoin.Strategies;

public abstract class BitcoinStrategyBase(string defaultBaseFilename, bool serializeCompressed) 
    : StrategyBase(defaultBaseFilename, serializeCompressed)
{ }