using EBA.Graph.Db.Neo4jDb;
using System.IO.Compression;

namespace EBA.Graph.Bitcoin.Strategies;

public class BitcoinStrategyFactory : IStrategyFactory
{
    private bool _disposed = false;

    public IReadOnlyDictionary<NodeKind, StrategyBase> NodeStrategies { get; }
    public IReadOnlyDictionary<EdgeKind, StrategyBase> EdgeStrategies { get; }

    public BitcoinStrategyFactory(Options options)
    {
        var compressOutput = options.Neo4j.CompressOutput;

        NodeStrategies = new Dictionary<NodeKind, StrategyBase>
        {
            { BlockNode.Kind, new BlockNodeStrategy(compressOutput) },
            { ScriptNode.Kind, new ScriptNodeStrategy(compressOutput) },
            { TxNode.Kind, new TxNodeStrategy(compressOutput) }
        };

        EdgeStrategies = new Dictionary<EdgeKind, StrategyBase>
        {
            { C2TEdge.Kind, new C2TEdgeStrategy(compressOutput) },
            { T2TEdge.KindTransfers, new T2TEdgeStrategy(T2TEdge.KindTransfers, compressOutput) },
            { T2TEdge.KindFee, new T2TEdgeStrategy(T2TEdge.KindFee, compressOutput) },
            { S2TEdge.Kind, new S2TEdgeStrategy(compressOutput) },
            { T2SEdge.Kind, new T2SEdgeStrategy(compressOutput) },
            { B2TEdge.Kind, new B2TEdgeStrategy(compressOutput) }
        };
    }

    public StrategyBase? GetStrategy(NodeKind kind)
    {
        if (NodeStrategies.TryGetValue(kind, out var strategy))
            return strategy;
        else
            return null;
    }

    public StrategyBase? GetStrategy(EdgeKind kind)
    {
        if (EdgeStrategies.TryGetValue(kind, out var strategy))
            return strategy;
        else
            return null;
    }

    public bool IsSerializable(NodeKind kind)
    {
        return NodeStrategies.ContainsKey(kind);
    }

    public bool IsSerializable(EdgeKind kind)
    {
        return EdgeStrategies.ContainsKey(kind);
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

        var strategies = NodeStrategies.Values.Concat(EdgeStrategies.Values);
        foreach (var strategy in strategies)
        {
            using var writer = new StreamWriter(
                new GZipStream(
                    File.Create(Path.Join(outputDirectory, $"header_{strategy.DefaultFilename}")),
                    CompressionMode.Compress));
            writer.WriteLine(strategy.GetCsvHeader());
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

        var txidIndex =
            $"// Create Txid index." +
            $"\r\nCREATE INDEX tx_txid_index IF NOT EXISTS " +
            $"\r\nFOR (t:{TxNode.Kind}) ON (t.{nameof(TxNode.Txid)});";
        writer.WriteLine("");
        writer.WriteLine(txidIndex);

        var followsEdge = 
            $"// Create edge (Block)-[{RelationType.Follows}]->(Block)" +
            $"\r\nMATCH (target:Block), (source:Block)" +
            $"\r\nWHERE target.{heightName} + 1 = source.{heightName}" +
            $"\r\nMERGE (target)-[:{RelationType.Follows}]->(source)";
        writer.WriteLine("");
        writer.WriteLine(followsEdge);
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
                foreach (var x in NodeStrategies)
                    x.Value.Dispose();

                foreach(var x in EdgeStrategies)
                    x.Value.Dispose();
            }

            _disposed = true;
        }
    }
}