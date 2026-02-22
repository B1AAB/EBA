using EBA.Graph.Db.Neo4jDb;
using System.IO.Compression;

namespace EBA.Graph.Bitcoin.Strategies;

public class BitcoinStrategyFactory : IStrategyFactory
{
    private bool _disposed = false;

    public IReadOnlyDictionary<Type, StrategyBase> Strategies { get; }

    public BitcoinStrategyFactory(Options options)
    {
        var compressOutput = options.Neo4j.CompressOutput;

        Strategies = new Dictionary<Type, StrategyBase>
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

    public StrategyBase? GetStrategy(Type type)
    {
        if (Strategies.TryGetValue(type, out var strategy))
            return strategy;
        else
            return null;
    }

    public bool IsSerializable(Type type)
    {
        return Strategies.ContainsKey(type);
    }

    public async Task SerializeConstantsAsync(string outputDirectory, CancellationToken ct)
    {
        // Serialize Coinbase Node
        using (var writer = new StreamWriter(
            new GZipStream(
                File.Create(Path.Join(outputDirectory, $"{CoinbaseNode.Kind}.csv.gz")),
                CompressionMode.Compress)))
        {
            writer.WriteLine(string.Join('\t', $"{CoinbaseNode.Kind}:ID({CoinbaseNode.Kind})", ":LABEL"));
            writer.WriteLine(string.Join('\t', $"{CoinbaseNode.Kind}", $"{CoinbaseNode.Kind}"));
        }

        foreach(var strategy in Strategies)
        {
            using var writer = new StreamWriter(
                new GZipStream(
                    File.Create(Path.Join(outputDirectory, $"header_{strategy.Value.DefaultFilename}")),
                    CompressionMode.Compress));
            writer.WriteLine(strategy.Value.GetCsvHeader());
        }
    }

    public async Task SerializeSchemasAsync(string outputDirectory, CancellationToken ct)
    {
        using var writer = new StreamWriter(File.Create(Path.Join(outputDirectory, "schema.cypher")));
        writer.WriteLine("// EBA Bitcoin Graph Schema");

        var x = PropertyMappingFactory.ScriptSHA256HashString<ScriptNode>(n => n.SHA256Hash).Property.Name;
        var scriptAddressUniqueness =
            $"// Uniqueness constraint for {ScriptNode.Kind}.{x} property." +
            $"\r\nCREATE CONSTRAINT {ScriptNode.Kind}_{x}_Unique " +
            $"\r\nIF NOT EXISTS " +
            $"\r\nFOR (v:{ScriptNode.Kind}) REQUIRE v.{x} IS UNIQUE;";
        writer.WriteLine("");
        writer.WriteLine(scriptAddressUniqueness);


        var txidName = PropertyMappingFactory.TxId<TxNode>(n => n.Txid).Property.Name;
        var txidUniqueness =
            $"// Uniqueness constraint for {TxNode.Kind}.{txidName} property." +
            $"\r\nCREATE CONSTRAINT {TxNode.Kind}_{txidName}_Unique " +
            $"\r\nIF NOT EXISTS " +
            $"\r\nFOR (v:{TxNode.Kind}) REQUIRE v.{txidName} IS UNIQUE;";
        writer.WriteLine("");
        writer.WriteLine(txidUniqueness);
        
        var heightName = PropertyMappingFactory.HeightProperty.Name;
        var blockHeightUniqueness =
            $"// Uniqueness constraint for {BlockNode.Kind}.{heightName} property." +
            $"\r\nCREATE CONSTRAINT {BlockNode.Kind}_{heightName}_Unique " +
            $"\r\nIF NOT EXISTS " +
            $"\r\nFOR (v:{BlockNode.Kind}) REQUIRE v.{heightName} IS UNIQUE;";
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
                foreach (var x in Strategies)
                {
                    x.Value.Dispose();
                }
            }

            _disposed = true;
        }
    }
}