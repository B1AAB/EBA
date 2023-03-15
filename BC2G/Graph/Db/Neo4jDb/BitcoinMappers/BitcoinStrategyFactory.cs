namespace BC2G.Graph.Db.Neo4jDb.BitcoinMappers;

public class BitcoinStrategyFactory : IStrategyFactory
{
    private bool _disposed = false;

    private readonly Dictionary<GraphComponentType, StrategyBase> _strategies;

    public BitcoinStrategyFactory()
    {
        _strategies = new()
        {
            {GraphComponentType.BitcoinGraph, new BlockGraphStrategy()},
            {GraphComponentType.BitcoinScriptNode, new ScriptNodeStrategy()},
            {GraphComponentType.BitcoinTxNode, new TxNodeStrategy()},
            {GraphComponentType.BitcoinC2T,  new C2TEdgeStrategy()},
            {GraphComponentType.BitcoinC2S,  new C2SEdgeStrategy()},
            {GraphComponentType.BitcoinS2S,  new S2SEdgeStrategy()},
            {GraphComponentType.BitcoinT2T, new T2TEdgeStrategy()}
        };
    }

    public StrategyBase GetStrategy(GraphComponentType type)
    {
        return _strategies[type];
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                foreach (var x in _strategies)
                {
                    x.Value.Dispose();
                }
            }

            _disposed = true;
        }
    }
}
