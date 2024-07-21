using BC2G.Utilities;

using System.Collections.Immutable;

namespace BC2G.Graph.Model;

public class GraphBase : IEquatable<GraphBase>, IGraphComponent, IDisposable
{
    private bool _disposed = false;

    public GraphComponentType ComponentType { get { return GraphComponentType.Graph; } }

    public int NodeCount
    {
        get { return (from x in _nodes select x.Value.Count).Sum(); }
    }
    public int EdgeCount
    {
        get { return (from x in _edges select x.Value.Count).Sum(); }
    }

    public ReadOnlyCollection<INode> Nodes
    {
        get
        {
            return new ReadOnlyCollection<INode>(
                _nodes.SelectMany(x => x.Value.Values).ToList());
        }
    }
    public ReadOnlyCollection<IEdge<INode, INode>> Edges
    {
        get
        {
            return new ReadOnlyCollection<IEdge<INode, INode>>(
                _edges.SelectMany(x => x.Value.Values).ToList());
        }
    }

    private readonly ConcurrentDictionary<GraphComponentType, ConcurrentDictionary<string, INode>> _nodes = new();
    private readonly ConcurrentDictionary<GraphComponentType, ConcurrentDictionary<string, IEdge<INode, INode>>> _edges = new();

    public ReadOnlyCollection<double> Labels
    {
        get { return new ReadOnlyCollection<double>(_labels); }
    }
    private readonly List<double> _labels = new();

    public int GetNodeCount(GraphComponentType type)
    {
        if (_nodes.TryGetValue(type, out ConcurrentDictionary<string, INode>? value))
            return value.Values.Count;
        return 0;
    }

    public ImmutableDictionary<GraphComponentType, ICollection<INode>> GetNodes()
    {
        return _nodes.ToImmutableDictionary(x => x.Key, x => x.Value.Values);
    }

    public ImmutableDictionary<GraphComponentType, ICollection<IEdge<INode, INode>>> GetEdges()
    {
        return _edges.ToImmutableDictionary(x => x.Key, x => x.Value.Values);
    }

    public bool TryAddNode<T>(GraphComponentType type, T node) where T : INode
    {
        var x = _nodes.GetOrAdd(
            type,
            new ConcurrentDictionary<string, INode>());

        return x.TryAdd(node.Id, node);
    }

    public T GetOrAddNode<T>(GraphComponentType type, T node) where T : INode
    {
        var x = _nodes.GetOrAdd(
            type,
            new ConcurrentDictionary<string, INode>());

        return (T)x.AddOrUpdate(node.Id, node, (key, oldValue) => node);
        // TODO: any better update logic?!
    }

    public void AddNodes<T>(GraphComponentType type, IEnumerable<T> nodes) where T : INode
    {
        foreach (var node in nodes)
            GetOrAddNode(type, node);
    }

    public T GetOrAddEdge<T>(GraphComponentType type, T edge) where T : IEdge<INode, INode>
    {
        var x = _edges.GetOrAdd(
            type,
            new ConcurrentDictionary<string, IEdge<INode, INode>>());

        return (T)x.GetOrAdd(edge.Id, edge);
    }

    public void AddEdges<T>(GraphComponentType type, IEnumerable<T> edges)
        where T : IEdge<INode, INode>
    {
        foreach (var edge in edges)
            GetOrAddEdge(type, edge);
    }

    public void AddOrUpdateEdge<T>(
        T edge, Func<string, IEdge<INode, INode>, IEdge<INode, INode>> updateValueFactory,
        GraphComponentType sourceType,
        GraphComponentType targetType,
        GraphComponentType edgeType)
        where T : IEdge<INode, INode>
    {
        var x = _edges.GetOrAdd(
            edgeType,
            new ConcurrentDictionary<string, IEdge<INode, INode>>());

        x.AddOrUpdate(edge.Id, edge, updateValueFactory);

        TryAddNode(sourceType, edge.Source);
        TryAddNode(targetType, edge.Target);
    }

    public List<T>? GetEdges<T>(GraphComponentType type) where T : IEdge<INode, INode>
    {
        if (!_edges.ContainsKey(type))
            return null;

        return _edges[type].Cast<T>().ToList();
    }

    public void AddLabel(double label)
    {
        _labels.Add(label);
    }

    public GraphFeatures GetFeatures()
    {
        return new GraphFeatures(this);
    }

    public void WriteFeatures(
        string outputDir,
        string nodeFeaturesFilename = "node_features.tsv",
        string edgeFeaturesFilename = "edge_features.tsv",
        string pairIndicesFilename = "pair_indices.tsv",
        string labelsFilename = "labels.tsv")
    {
        var x = GetFeatures();
        Directory.CreateDirectory(outputDir);

        Helpers.CsvSerialize(
            x.NodeFeatures,
            Path.Join(outputDir, nodeFeaturesFilename),
            x.NodeFeaturesHeader);

        Helpers.CsvSerialize(
            x.EdgeFeatures,
            Path.Join(outputDir, edgeFeaturesFilename),
            x.EdgeFeaturesHeader);

        Helpers.CsvSerialize(
            x.PairIndices,
            Path.Join(outputDir, pairIndicesFilename),
            x.PairIndicesHeader);

        Helpers.CsvSerialize(
            x.Labels,
            Path.Combine(outputDir, labelsFilename),
            x.LabelsHeader);
    }

    public bool Equals(GraphBase? other)
    {
        if (other == null)
            return false;

        return ReferenceEquals(this, other);
    }
    public override bool Equals(object? other)
    {
        return Equals(other as GraphBase);
    }
    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            { }
        }

        _disposed = true;
    }
}
