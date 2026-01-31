
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
                File.Create(Path.Join(outputDirectory, "BitcoinCoinbase.csv.gz")),
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

    public async Task SerializeSchemasAsync(string outputDirectory, CancellationToken ct)
    {
        using var writer = new StreamWriter(File.Create(Path.Join(outputDirectory, "schema.cypher")));
        writer.WriteLine("// EBA Bitcoin Graph Schema");

        var x = PropertyMappingFactory.Address<ScriptNode>(n => n.Address).Property.Name;
        var scriptAddressUniqueness =
            $"// Uniqueness constraint for {NodeLabels.Script}.{x} property." +
            $"\r\nCREATE CONSTRAINT {NodeLabels.Script}_{x}_Unique " +
            $"\r\nIF NOT EXISTS " +
            $"\r\nFOR (v:{NodeLabels.Script}) REQUIRE v.{x} IS UNIQUE;";
        writer.WriteLine("");
        writer.WriteLine(scriptAddressUniqueness);


        var txidName = PropertyMappingFactory.TxId<TxNode>(n => n.Txid).Property.Name;
        var txidUniqueness =
            $"// Uniqueness constraint for {NodeLabels.Tx}.{txidName} property." +
            $"\r\nCREATE CONSTRAINT {NodeLabels.Tx}_{txidName}_Unique " +
            $"\r\nIF NOT EXISTS " +
            $"\r\nFOR (v:{NodeLabels.Tx}) REQUIRE v.{txidName} IS UNIQUE;";
        writer.WriteLine("");
        writer.WriteLine(txidUniqueness);
        
        var heightName = PropertyMappingFactory.HeightProperty.Name;
        var blockHeightUniqueness =
            $"// Uniqueness constraint for {NodeLabels.Block}.{heightName} property." +
            $"\r\nCREATE CONSTRAINT {NodeLabels.Block}_{heightName}_Unique " +
            $"\r\nIF NOT EXISTS " +
            $"\r\nFOR (v:{NodeLabels.Block}) REQUIRE v.{heightName} IS UNIQUE;";
        writer.WriteLine("");
        writer.WriteLine(blockHeightUniqueness);
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