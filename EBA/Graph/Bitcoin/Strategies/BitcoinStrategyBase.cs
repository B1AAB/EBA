using EBA.Graph.Db.Neo4jDb;

namespace EBA.Graph.Bitcoin.Strategies;

public abstract class BitcoinStrategyBase(bool serializeCompressed) : StrategyBase(serializeCompressed)
{ }