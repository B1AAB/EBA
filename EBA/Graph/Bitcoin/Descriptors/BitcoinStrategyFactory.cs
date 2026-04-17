using EBA.Graph.Db.Neo4jDb;
using System.IO.Compression;

namespace EBA.Graph.Bitcoin.Descriptors;

public class BitcoinStrategyFactory : IStrategyFactory
{
    private bool _disposed = false;

    public IReadOnlyDictionary<NodeKind, IElementCodec> NodeStrategies { get; }
    public IReadOnlyDictionary<EdgeKind, IElementCodec> EdgeStrategies { get; }

    public BitcoinStrategyFactory(Options options)
    {
        var compressOutput = options.Neo4j.CompressOutput;

        NodeStrategies = new Dictionary<NodeKind, IElementCodec>
        {
            {
                BlockNode.Kind,
                new Neo4jCodec<BlockNode>(new BlockNodeDescriptor(), KindToFilename(BlockNode.Kind), compressOutput)
            },
            {
                ScriptNode.Kind,
                new Neo4jCodec<ScriptNode>(new ScriptNodeDescriptor(), KindToFilename(ScriptNode.Kind), compressOutput)
            },
            {
                TxNode.Kind,
                new Neo4jCodec<TxNode>(new TxNodeDescriptor(), KindToFilename(TxNode.Kind), compressOutput)
            }
        };

        EdgeStrategies = new Dictionary<EdgeKind, IElementCodec>
        {
            {
                C2TEdge.Kind,
                new Neo4jCodec<C2TEdge>(new C2TEdgeDescriptor(), KindToFilename(C2TEdge.Kind), compressOutput)
            },
            {
                T2TEdge.KindTransfers,
                new Neo4jCodec<T2TEdge>(new T2TEdgeDescriptor(), KindToFilename(T2TEdge.KindTransfers), compressOutput)
            },
            {
                T2TEdge.KindFee,
                new Neo4jCodec<T2TEdge>(new T2TEdgeDescriptor(), KindToFilename(T2TEdge.KindFee), compressOutput)
            },
            {
                S2TEdge.Kind,
                new Neo4jCodec<S2TEdge>(new S2TEdgeDescriptor(), KindToFilename(S2TEdge.Kind), compressOutput)
            },
            {
                T2SEdge.Kind,
                new Neo4jCodec<T2SEdge>(new T2SEdgeDescriptor(), KindToFilename(T2SEdge.Kind), compressOutput)
            },
            {
                B2TEdge.Kind,
                new Neo4jCodec<B2TEdge>(new B2TEdgeDescriptor(), KindToFilename(B2TEdge.Kind), compressOutput)
            },
            {
                B2BEdge.Kind,
                new Neo4jCodec<B2BEdge>(new B2BEdgeDescriptor(), KindToFilename(B2BEdge.Kind), compressOutput)
            }
        };
    }

    private static string KindToFilename(NodeKind kind) => $"nodes_{kind}";
    private static string KindToFilename(EdgeKind kind) => $"edges_{kind.Source}_{kind.Relation}_{kind.Target}";


    public IElementCodec? GetStrategy(NodeKind kind)
    {
        if (NodeStrategies.TryGetValue(kind, out var strategy))
            return strategy;
        else
            return null;
    }

    public IElementCodec? GetStrategy(EdgeKind kind)
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
        // serialize Coinbase Node
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

        var x = ((Neo4jCodec<ScriptNode>)NodeStrategies[ScriptNode.Kind]).Descriptor.Mapper.GetMapping(x => x.SHA256Hash).Property.Name;
        var scriptAddressUniqueness =
            $"// Uniqueness constraint for {ScriptNode.Kind}.{x} property." +
            $"\r\nCREATE CONSTRAINT {ScriptNode.Kind}_{x}_Unique " +
            $"\r\nIF NOT EXISTS " +
            $"\r\nFOR (v:{ScriptNode.Kind}) REQUIRE v.{x} IS UNIQUE;";
        writer.WriteLine("");
        writer.WriteLine(scriptAddressUniqueness);

        var txidName = ((Neo4jCodec<TxNode>)NodeStrategies[TxNode.Kind]).Descriptor.Mapper.GetMapping(x => x.Txid).Property.Name;
        var txidUniqueness =
            $"// Uniqueness constraint for {TxNode.Kind}.{txidName} property." +
            $"\r\nCREATE CONSTRAINT {TxNode.Kind}_{txidName}_Unique " +
            $"\r\nIF NOT EXISTS " +
            $"\r\nFOR (v:{TxNode.Kind}) REQUIRE v.{txidName} IS UNIQUE;";
        writer.WriteLine("");
        writer.WriteLine(txidUniqueness);

        var heightName = ((Neo4jCodec<BlockNode>)NodeStrategies[BlockNode.Kind]).Descriptor.Mapper.GetMapping(x => x.BlockMetadata.Height).Property.Name;
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

    public bool TryCreateNode<T>(
        Neo4j.Driver.INode node,
        out T createdNode,
        double? originalIndegree = null,
        double? originalOutdegree = null,
        double? outHopsFromRoot = null) where T : Model.INode
    {
        createdNode = default!;

        var props = node.Properties;
        var id = node.ElementId.ToString();

        Model.INode parsedNode;
        bool success = true;

        if (node.Labels.Contains(ScriptNode.Kind.ToString()))
        {
            parsedNode = ScriptNodeDescriptor.Deserialize(
                props, originalIndegree, originalOutdegree, outHopsFromRoot, id);
        }
        else if (node.Labels.Contains(TxNode.Kind.ToString()))
        {
            parsedNode = TxNodeDescriptor.Deserialize(
                props, originalIndegree, originalOutdegree, outHopsFromRoot, id);
            success = !((TxNode)parsedNode).HasNullFeatures();
        }
        else if (node.Labels.Contains(BlockNode.Kind.ToString()))
        {
            parsedNode = BlockNodeDescriptor.Deserialize(
                props, originalIndegree, originalOutdegree, outHopsFromRoot, id);
        }
        else if (node.Labels.Contains(CoinbaseNode.Kind.ToString()))
        {
            parsedNode = new CoinbaseNode(
                originalOutdegree, outHopsFromRoot, id);
        }
        else
        {
            throw new NotImplementedException(
                $"Unexpected node type, labels: {string.Join(',', node.Labels)}");
        }

        if (success && parsedNode is T typedNode)
        {
            createdNode = typedNode;
            return true;
        }

        return false;
    }

    public bool TryCreateNode(
            Neo4j.Driver.INode node,
            out Model.INode createdNode,
            double? originalIndegree = null,
            double? originalOutdegree = null,
            double? outHopsFromRoot = null)
    {
        return TryCreateNode<Model.INode>(
            node,
            out createdNode,
            originalIndegree,
            originalOutdegree,
            outHopsFromRoot);
    }

    public bool TryCreateNodes<T>(
        List<IRecord> records, 
        out List<T> nodes, 
        string nodeVar = "n") where T : Model.INode
    {
        nodes = [];
        foreach (var record in records)
        {
            if (!TryCreateNode(record[nodeVar].As<Neo4j.Driver.INode>(), out T createdNode))
                return false;

            nodes.Add(createdNode);
        }

        return true;
    }

    public IEdge<Model.INode, Model.INode> CreateEdge(
        Model.INode source,
        Model.INode target,
        IRelationship relationship)
    {
        var properties = relationship.Properties;

        return (source, target) switch
        {
            (CoinbaseNode, TxNode v) => C2TEdgeDescriptor.Deserialize(v, properties),
            (TxNode u, TxNode v) => T2TEdgeDescriptor.Deserialize(u, v, properties),
            (BlockNode u, TxNode v) => B2TEdgeDescriptor.Deserialize(u, v, properties),
            (TxNode u, ScriptNode v) => T2SEdgeDescriptor.Deserialize(u, v, properties),
            (ScriptNode u, TxNode v) => S2TEdgeDescriptor.Deserialize(u, v, properties),
            (BlockNode u, BlockNode v) => B2BEdgeDescriptor.Deserialize(u, v),

            _ => throw new ArgumentException(
                $"Invalid edge type combination: {source.GetType().Name} -> {target.GetType().Name}")
        };
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

                foreach (var x in EdgeStrategies)
                    x.Value.Dispose();
            }

            _disposed = true;
        }
    }
}