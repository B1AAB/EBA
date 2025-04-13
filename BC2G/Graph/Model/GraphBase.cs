using BC2G.Utilities;

using System.Collections.Immutable;

namespace BC2G.Graph.Model;

public class GraphBase(string? id = null) : IEquatable<GraphBase>, IGraphComponent, IDisposable
{
    public string Id { get; } = id == null ?  Helpers.GetTimestamp() : id.Trim();

    private bool _disposed = false;

    public static GraphComponentType ComponentType { get { return GraphComponentType.Graph; } }
    public GraphComponentType GetGraphComponentType() => ComponentType;

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
    private readonly List<double> _labels = [];

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

    public void GetNode(string id, out INode node, out GraphComponentType graphComponentType)
    {
        foreach (var nodeTypes in _nodes)
        {
            nodeTypes.Value.TryGetValue(id, out var n);
            if (n != null)
            {
                node = n;
                graphComponentType = nodeTypes.Key;
                return;
            }
        }

        throw new NotImplementedException();
    }

    public void GetEdge(string id, out IEdge<INode, INode> edge, out GraphComponentType graphComponentType)
    {
        foreach (var edgeTypes in _edges)
        {
            edgeTypes.Value.TryGetValue(id, out var e);
            if (e != null)
            {
                edge = e;
                graphComponentType = edgeTypes.Key;
                return;
            }
        }

        throw new NotImplementedException();
    }

    public bool TryAddNode<T>(GraphComponentType type, T node) where T : INode
    {
        // TODO: this is a hotspot 
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

        edge.Source.AddOutgoingEdge(edge);
        edge.Target.AddIncomingEdge(edge);

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

    public void Serialize(
        string workingDir, 
        string perBatchLabelsFilename, 
        bool serializeFeatureVectors = true, 
        bool serializeEdges = false)
    {
        Directory.CreateDirectory(workingDir);

        if (serializeFeatureVectors)
            SerializeFeatures(workingDir, perBatchLabelsFilename);

        if (serializeEdges)
            SerializeEdges(workingDir);
    }

    public void SerializeEdges(string workingDir, string edgesFilename = "edges.tsv")
    {
        var header = new[] { "SourceId", "TargetId", "SourceNodeType", "TargetNodeType", "EdgeValue", "EdgeType" };

        var edges = _edges.Values.SelectMany(ids => ids.Values).Select(
            edges => new[]
            {
                edges.Source.GetUniqueLabel(),
                edges.Target.GetUniqueLabel(),
                edges.Source.GetGraphComponentType().ToString(),
                edges.Target.GetGraphComponentType().ToString(),
                edges.Value.ToString(),
                edges.Type.ToString()
            });

        Helpers.CsvSerialize(edges, Path.Combine(workingDir, edgesFilename), header, append: true);
        Helpers.CsvSerialize(edges, Path.Combine(workingDir, edgesFilename), header);
    }

    public GraphFeatures GetFeatures()
    {
        return new GraphFeatures(this);
    }

    public void SerializeFeatures(
        string workingDir,
        string perBatchLabelsFilename,
        string perGraphLabelsFilename = "Labels.tsv")
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


    public void DownSample(int maxNodesCount, int maxEdgesCount, int? seed = null)
    {
        // TODO: this sampling is not ideal;
        // (1) it is not the fastest;
        // (2) it may lead to having fewer nodes/edges than requested;
        // (3) most importantly, it removes nodes/edges independent of
        //     "path" so it may lead to turning a graph into more than one subgraphs.
        //
        // For a better sampling algorithm, use "Reservoir sampling". Ref: 
        // - https://stackoverflow.com/a/48089/947889
        // - https://en.wikipedia.org/wiki/Reservoir_sampling
        //

        Random rnd = seed == null ? new Random() : new Random((int)seed);
        var nodesToRemoveCount = NodeCount - maxNodesCount;
        if (nodesToRemoveCount > 0)
        {
            var allNodesIds = _nodes.SelectMany(x => x.Value.Select(y => new object[] { x.Key, y.Key })).ToList();
            var nodesToRemove = allNodesIds.OrderBy(x => rnd.Next()).Take(nodesToRemoveCount);

            foreach (var nodeToRemove in nodesToRemove)
            {
                _nodes[(GraphComponentType)nodeToRemove[0]].Remove((string)nodeToRemove[1], out var removedNode);

                if (removedNode != null)
                {
                    foreach (var e in removedNode.IncomingEdges)
                        _edges[e.GetGraphComponentType()].Remove(e.Id, out _);

                    foreach (var e in removedNode.OutgoingEdges)
                        _edges[e.GetGraphComponentType()].Remove(e.Id, out _);
                }
            }
        }

        var edgesToRemoveCount = EdgeCount - maxEdgesCount;
        if (edgesToRemoveCount > 0)
        {
            var allEdgesIds = _edges.SelectMany(x => x.Value.Select(y => new object[] { x.Key, y.Key })).ToList();
            var edgesToRemove = allEdgesIds.OrderBy(x => rnd.Next()).Take(edgesToRemoveCount);

            foreach (var edgeToRemove in edgesToRemove)
                _edges[(GraphComponentType)edgeToRemove[0]].Remove((string)edgeToRemove[1], out _);
        }

        var disconnectNodes = _nodes
            .SelectMany(t => t.Value.Where(n => n.Value.InDegree == 0 & n.Value.OutDegree == 0)
            .Select(x => new object[] { t.Key, x.Key }));

        foreach (var node in disconnectNodes)
            _nodes[(GraphComponentType)node[0]].Remove((string)node[1], out _);
    }

    public void DownSample(int maxEdgesCount, int? seed = null)
    {
        // TODO: this sampling is not ideal
        // because it can be very slow
        //
        // For a better sampling algorithm, use "Reservoir sampling". Ref: 
        // - https://stackoverflow.com/a/48089/947889
        // - https://en.wikipedia.org/wiki/Reservoir_sampling
        //

        Random rnd = seed == null ? new Random() : new Random((int)seed);
        var edgesToRemoveCount = EdgeCount - maxEdgesCount;

        if (edgesToRemoveCount > 0)
        {
            var removedEdgesCounter = 0;
            foreach(var edgeType in _edges)
            {
                foreach(var edge in edgeType.Value)
                {

                }
            }

            var allEdgesIds = _edges.SelectMany(x => x.Value.Select(y => new object[] { x.Key, y.Key })).ToList();
            var edgesToRemove = allEdgesIds.OrderBy(x => rnd.Next()).Take(edgesToRemoveCount);

            foreach (var edgeToRemove in edgesToRemove)
                _edges[(GraphComponentType)edgeToRemove[0]].Remove((string)edgeToRemove[1], out _);
        }
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
