using BC2G.Utilities;

using System.Collections.Immutable;

namespace BC2G.Graph.Model;

public class GraphBase : IEquatable<GraphBase>, IGraphComponent, IDisposable
{
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
        string baseOutputDir,
        string outputDir, 
        string edgesFilename = "edges.tsv",
        string nodeFeaturesFilename = "node_features.tsv",
        string edgeFeaturesFilename = "edge_features.tsv",
        string pairIndicesFilename = "pair_indices.tsv",
        string labelsFilename = "labels.tsv")
    {
        SerializeFeatures(outputDir, nodeFeaturesFilename, edgeFeaturesFilename, pairIndicesFilename, labelsFilename);
        SerializeEdges(workingDir, baseOutputDir, edgesFilename);
    }

    public void SerializeEdges(string workingDir, string baseOutputDir, string edgesFilename = "edges.tsv")
    {
        Directory.CreateDirectory(baseOutputDir);
        var header = new[] { "SourceId", "TargetId", "SourceNodeType", "TargetNodeType", "EdgeValue", "EdgeType" };

        var edges = _edges.Values.SelectMany(ids => ids.Values).Select(
            edges => new[]
            {
                edges.Source.Id,
                edges.Target.Id,
                edges.Source.GetGraphComponentType().ToString(),
                edges.Target.GetGraphComponentType().ToString(),
                edges.Value.ToString(),
                edges.Type.ToString()
            });

        Helpers.CsvSerialize(edges, Path.Combine(workingDir, edgesFilename), header, append: true);
        Helpers.CsvSerialize(edges, Path.Combine(baseOutputDir, edgesFilename), header);
    }

    public GraphFeatures GetFeatures()
    {
        return new GraphFeatures(this);
    }

    public void SerializeFeatures(
        string outputDir,
        string nodeFeaturesFilename = "node_features.tsv",
        string edgeFeaturesFilename = "edge_features.tsv",
        string pairIndicesFilename = "pair_indices.tsv",
        string labelsFilename = "labels.tsv")
    {
        Directory.CreateDirectory(outputDir);

        var gFeatures = GetFeatures();

        foreach (var nodeType in gFeatures.NodeFeatures)
        {
            Helpers.CsvSerialize(
                nodeType.Value,
                Path.Join(outputDir, nodeType.Key + nodeFeaturesFilename),
                gFeatures.NodeFeaturesHeader[nodeType.Key]);
        }

        Helpers.CsvSerialize(
            gFeatures.EdgeFeatures,
            Path.Join(outputDir, edgeFeaturesFilename),
            gFeatures.EdgeFeaturesHeader);

        Helpers.CsvSerialize(
            gFeatures.PairIndices,
            Path.Join(outputDir, pairIndicesFilename),
            gFeatures.PairIndicesHeader);

        Helpers.CsvSerialize(
            gFeatures.Labels,
            Path.Combine(outputDir, labelsFilename),
            gFeatures.LabelsHeader);
    }


    public void Downsample(int maxNodesCount, int maxEdgesCount, int? seed = null)
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
