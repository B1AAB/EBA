namespace BC2G.Graph.Db.Neo4jDb.BitcoinStrategies;

public class BitcoinStrategyFactory : IStrategyFactory
{
    private bool _disposed = false;

    private readonly Dictionary<GraphComponentType, StrategyBase> _strategies;

    public BitcoinStrategyFactory(Options options)
    {
        var compressOutput = options.Neo4j.CompressOutput;
        _strategies = new()
        {
            {GraphComponentType.BitcoinGraph, new BlockNodeStrategy(compressOutput)},
            {GraphComponentType.BitcoinScriptNode, new ScriptNodeStrategy(compressOutput)},
            {GraphComponentType.BitcoinTxNode, new TxNodeStrategy(compressOutput)},
            {GraphComponentType.BitcoinC2T,  new C2TEdgeStrategy(compressOutput)},
            {GraphComponentType.BitcoinC2S,  new C2SEdgeStrategy(compressOutput)},
            {GraphComponentType.BitcoinS2S,  new S2SEdgeStrategy(compressOutput)},
            {GraphComponentType.BitcoinT2T, new T2TEdgeStrategy(compressOutput)}
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
