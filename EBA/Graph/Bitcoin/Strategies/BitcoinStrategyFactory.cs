using EBA.Graph.Db.Neo4jDb;
using System.IO.Compression;

namespace EBA.Graph.Bitcoin.Strategies;

public class BitcoinStrategyFactory : IStrategyFactory
{
    private bool _disposed = false;

    private readonly Dictionary<Type, StrategyBase> _strategies;

    public BitcoinStrategyFactory(Options options)
    {
        var compressOutput = options.Neo4j.CompressOutput;
        _strategies = new()
        {
            {typeof(BlockNode), new BlockNodeStrategy(compressOutput)},
            {typeof(ScriptNode), new ScriptNodeStrategy(compressOutput)},
            {typeof(TxNode), new TxNodeStrategy(compressOutput)},
            {typeof(C2TEdge), new C2TEdgeStrategy(compressOutput)},
            {typeof(T2TEdge), new T2TEdgeStrategy(compressOutput)},
            {typeof(S2TEdge), new S2TEdgeStrategy(compressOutput)},
            {typeof(T2SEdge), new T2SEdgeStrategy(compressOutput)},
            {typeof(B2TEdge), new B2TEdgeStrategy(compressOutput)},
        };
    }

    public StrategyBase GetStrategy(Type type)
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
            writer.WriteLine(string.Join('\t', $"{NodeKind.Coinbase}:ID({NodeKind.Coinbase})", ":LABEL"));
            writer.WriteLine(string.Join('\t', $"{NodeKind.Coinbase}", $"{NodeKind.Coinbase}"));
        }

        foreach(var strategy in _strategies)
        {
            using var writer = new StreamWriter(
                new GZipStream(
                    File.Create(Path.Join(outputDirectory, $"header_{strategy.Key.Name}.csv.gz")),
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
            $"// Uniqueness constraint for {NodeKind.Script}.{x} property." +
            $"\r\nCREATE CONSTRAINT {NodeKind.Script}_{x}_Unique " +
            $"\r\nIF NOT EXISTS " +
            $"\r\nFOR (v:{NodeKind.Script}) REQUIRE v.{x} IS UNIQUE;";
        writer.WriteLine("");
        writer.WriteLine(scriptAddressUniqueness);


        var txidName = PropertyMappingFactory.TxId<TxNode>(n => n.Txid).Property.Name;
        var txidUniqueness =
            $"// Uniqueness constraint for {NodeKind.Tx}.{txidName} property." +
            $"\r\nCREATE CONSTRAINT {NodeKind.Tx}_{txidName}_Unique " +
            $"\r\nIF NOT EXISTS " +
            $"\r\nFOR (v:{NodeKind.Tx}) REQUIRE v.{txidName} IS UNIQUE;";
        writer.WriteLine("");
        writer.WriteLine(txidUniqueness);
        
        var heightName = PropertyMappingFactory.HeightProperty.Name;
        var blockHeightUniqueness =
            $"// Uniqueness constraint for {NodeKind.Block}.{heightName} property." +
            $"\r\nCREATE CONSTRAINT {NodeKind.Block}_{heightName}_Unique " +
            $"\r\nIF NOT EXISTS " +
            $"\r\nFOR (v:{NodeKind.Block}) REQUIRE v.{heightName} IS UNIQUE;";
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