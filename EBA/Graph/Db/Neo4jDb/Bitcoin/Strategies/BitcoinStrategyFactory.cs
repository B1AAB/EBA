
using EBA.Graph.Bitcoin;
using System.IO.Compression;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class BitcoinStrategyFactory : IStrategyFactory
{
    private bool _disposed = false;

    private readonly Dictionary<GraphComponentType, StrategyBase> _strategies;

    public BitcoinStrategyFactory(Options options)
    {
        var compressOutput = options.Neo4j.CompressOutput;
        _strategies = new()
        {
            {GraphComponentType.BitcoinBlockNode, new BlockNodeStrategy(compressOutput)},
            {GraphComponentType.BitcoinScriptNode, new ScriptNodeStrategy(compressOutput)},
            {GraphComponentType.BitcoinTxNode, new TxNodeStrategy(compressOutput)},
            {GraphComponentType.BitcoinC2T, new C2TEdgeStrategy(compressOutput)},
            {GraphComponentType.BitcoinC2S, new C2SEdgeStrategy(compressOutput)},
            {GraphComponentType.BitcoinS2S, new S2SEdgeStrategy(compressOutput)},
            {GraphComponentType.BitcoinT2T, new T2TEdgeStrategy(compressOutput)},
            {GraphComponentType.BitcoinS2T, new S2TEdgeStrategy(compressOutput)},
            {GraphComponentType.BitcoinT2S, new T2SEdgeStrategy(compressOutput)},
            {GraphComponentType.BitcoinB2T, new B2TEdgeStrategy(compressOutput)}
        };
    }

    public StrategyBase GetStrategy(GraphComponentType type)
    {
        return _strategies[type];
    }

    public async Task SerializeConstantsAsync(string outputDirectory, CancellationToken ct)
    {
        // Serialize Coinbase Node
        using (var writer = new StreamWriter(
            new GZipStream(
                File.Create(Path.Join(outputDirectory, "BitcoinCoinbase.tsv.gz")),
                CompressionMode.Compress)))
        {
            writer.WriteLine(string.Join('\t', $"{NodeLabels.Coinbase}:ID({NodeLabels.Coinbase})", ":LABEL"));
            writer.WriteLine(string.Join('\t', $"{NodeLabels.Coinbase}", $"{NodeLabels.Coinbase}"));
        }

        foreach(var strategy in _strategies)
        {
            using var writer = new StreamWriter(
                new GZipStream(
                    File.Create(Path.Join(outputDirectory, $"header_{strategy.Key}.csv.gz")),
                    CompressionMode.Compress));
            writer.WriteLine(strategy.Value.GetCsvHeader());
        }
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