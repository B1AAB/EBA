using EBA.Utilities;

using System.Collections.Immutable;

namespace EBA.Graph.Model;

public class GraphBase(string? id = null) : IEquatable<GraphBase>, IDisposable
{
    public string Id { get; } = id == null ?  Helpers.GetTimestamp() : id.Trim();

    private bool _disposed = false;

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
                [.. _nodes.SelectMany(x => x.Value.Values)]);
        }
    }

    public Dictionary<Type, List<INode>> NodesByType
    {
        get
        {
            return _nodes.ToDictionary(
                x => x.Key, 
                x => new List<INode>(x.Value.Values));
        }
    }

    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, INode>> _nodes = new();

    public ReadOnlyCollection<IEdge<INode, INode>> Edges
    {
        get
        {
            return new ReadOnlyCollection<IEdge<INode, INode>>(
                [.. _edges.SelectMany(x => x.Value.Values)]);
        }
    }

    public Dictionary<Type, List<IEdge<INode, INode>>> EdgesByType
    {
        get
        {
            return _edges.ToDictionary(
                x => x.Key, 
                x => new List<IEdge<INode, INode>>(x.Value.Values));
        }
    } 
    
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, IEdge<INode, INode>>> _edges = new();

    public ReadOnlyDictionary<string, string> Labels
    {
        get { return new ReadOnlyDictionary<string, string>(_labels); }
    }
    private readonly Dictionary<string, string> _labels = [];

    public int GetNodeCount(Type type)
    {
        if (_nodes.TryGetValue(type, out ConcurrentDictionary<string, INode>? value))
            return value.Values.Count;
        return 0;
    }

    public ImmutableDictionary<Type, ICollection<INode>> GetNodes()
    {
        return _nodes.ToImmutableDictionary(x => x.Key, x => x.Value.Values);
    }

    public ImmutableDictionary<Type, ICollection<IEdge<INode, INode>>> GetEdges()
    {
        return _edges.ToImmutableDictionary(x => x.Key, x => x.Value.Values);
    }

    public void GetNode(string id, out INode node) // TODO: you can change this to return node instead of void
    {
        foreach (var nodeTypes in _nodes)
        {
            nodeTypes.Value.TryGetValue(id, out var n);
            if (n != null)
            {
                node = n;
                return;
            }
        }

        throw new NotImplementedException();
    }

    public void GetEdge(string id, out IEdge<INode, INode> edge) // TODO: you can change this to return edge instead of void
    {
        foreach (var edgeTypes in _edges)
        {
            edgeTypes.Value.TryGetValue(id, out var e);
            if (e != null)
            {
                edge = e;
                return;
            }
        }

        throw new NotImplementedException();
    }

    public bool ContainsNode(string id)
    {
        foreach (var nodeTypes in _nodes)
            if (nodeTypes.Value.ContainsKey(id))
                return true;

        return false;
    }

    public bool TryGetNode(string id, out INode? node)
    {
        foreach (var nodeTypes in _nodes)
            if (nodeTypes.Value.TryGetValue(id, out node))
                return true;
        node = null;
        return false;
    }

    public bool ContainsEdge(string id)
    {
        foreach (var edgeTypes in _edges)
            if (edgeTypes.Value.ContainsKey(id))
                return true;
        return false;
    }

    public bool TryAddNode<T>(T node) where T : INode
    {
        var x = _nodes.GetOrAdd(
            node.GetType(),
            new ConcurrentDictionary<string, INode>());

        return x.TryAdd(node.Id, node);
    }

    public T GetOrAddNode<T>(T node) where T : INode
    {
        var x = _nodes.GetOrAdd(
            node.GetType(),
            new ConcurrentDictionary<string, INode>());

        return (T)x.GetOrAdd(node.Id, node);
    }

    public void AddNodes<T>(IEnumerable<T> nodes) where T : INode
    {
        foreach (var node in nodes)
            GetOrAddNode(node);
    }

    public bool TryGetOrAddEdge<T>(T edge, out T resultingEdge) where T : IEdge<INode, INode>
    {
        var x = _edges.GetOrAdd(
            edge.GetType(),
            new ConcurrentDictionary<string, IEdge<INode, INode>>());

        resultingEdge = (T)x.GetOrAdd(edge.Id, edge);

        return ReferenceEquals(resultingEdge, edge);
    }

    public void AddOrUpdateEdge<T>(T edge, Func<T, T, T>? updateFunc = null)
        where T : IEdge<INode, INode>
    {
        var x = _edges.GetOrAdd(
            edge.GetType(),
            new ConcurrentDictionary<string, IEdge<INode, INode>>());

        x.AddOrUpdate(
            edge.Id,
            edge,
            (_, oldEdge) =>
            {
                if (updateFunc != null)
                    return updateFunc((T)oldEdge, edge);

                oldEdge.AddValue(edge.Value);
                return oldEdge;
            });

        edge.Source.AddOutgoingEdge(edge);
        edge.Target.AddIncomingEdge(edge);

        TryAddNode(edge.Source);
        TryAddNode(edge.Target);
    }

    public List<T>? GetEdges<T>(Type type) where T : IEdge<INode, INode>
    {
        if (!_edges.ContainsKey(type))
            return null;

        return [.. _edges[type].Cast<T>()];
    }

    public void AddLabel(string key, string value)
    {
        _labels[key] = value;
    }

    public void Serialize(
        string workingDir, 
        string perBatchLabelsFilename, 
        bool serializeFeatureVectors = true)
    {
        Directory.CreateDirectory(workingDir);

        if (serializeFeatureVectors)
            SerializeFeatures(workingDir, perBatchLabelsFilename);
    }

    public GraphFeatures GetFeatures()
    {
        return new GraphFeatures(this);
    }

    public void SerializeFeatures(
        string workingDir,
        string perBatchLabelsFilename,
        string perGraphLabelsFilename = "metadata.tsv")
    {
        var gFeatures = GetFeatures();

        foreach (var nodeType in gFeatures.NodeFeatures)
        {
            if (nodeType.Value.Count == 0)
                continue;

            Helpers.CsvSerialize(
                nodeType.Value,
                Path.Join(workingDir, nodeType.Key + ".tsv"),
                gFeatures.NodeFeaturesHeader[nodeType.Key]);
        }

        foreach (var edgeType in gFeatures.EdgeFeatures)
        {
            if (edgeType.Value.Count == 0)
                continue;

            Helpers.CsvSerialize(
                edgeType.Value,
                Path.Join(workingDir, edgeType.Key + ".tsv"),
                gFeatures.EdgeFeaturesHeader[edgeType.Key]);
        }

        Helpers.CsvSerialize(
            [gFeatures.Labels.ToArray()],
            Path.Combine(workingDir, perBatchLabelsFilename),
            gFeatures.LabelsHeader,
            append: true);

        Helpers.CsvSerialize(
            [gFeatures.Labels.ToArray()],
            Path.Combine(workingDir, perGraphLabelsFilename),
            gFeatures.LabelsHeader);
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
