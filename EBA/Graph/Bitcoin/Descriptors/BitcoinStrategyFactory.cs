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

        var strategies = 
            NodeStrategies.Select(n => (Label: $"{n.Key} nodes", Codec: n.Value))
            .Concat(
            EdgeStrategies.Select(e => (Label: $"{e.Key} edges", Codec: e.Value)));

        foreach (var (label, codec) in strategies)
        {
            var configs = new List<string>();
            configs.AddRange(codec.GetSchemaConfigs());
            configs.AddRange(codec.GetSeedingCommands());

            if (configs.Count > 0)
            {
                writer.WriteLine("");
                writer.WriteLine("// -----------------------------------------------------");
                writer.WriteLine($"// Schema for {label}");
                writer.WriteLine("// -----------------------------------------------------");

                foreach (var config in configs)
                {
                    writer.WriteLine("");
                    writer.WriteLine(config);
                }

                writer.WriteLine("");
            }
        }
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