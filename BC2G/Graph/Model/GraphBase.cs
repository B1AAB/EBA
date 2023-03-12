using System.Collections.Immutable;

namespace BC2G.Graph.Model;

public class GraphBase : IEquatable<GraphBase>, IGraphComponent
{
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
            type,//Utilities.TypeToString(node), 
            new ConcurrentDictionary<string, INode>());

        return x.TryAdd(node.Id, node);
    }

    public T GetOrAddNode<T>(GraphComponentType type, T node) where T : INode
    {
        var x = _nodes.GetOrAdd(
            type,//Utilities.TypeToString(node),
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
            //Utilities.TypeToString(edge.GetType()), 
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
            //Utilities.TypeToString(edge.GetType()),
            edgeType,
            new ConcurrentDictionary<string, IEdge<INode, INode>>());

        x.AddOrUpdate(edge.Id, edge, updateValueFactory);

        TryAddNode(sourceType, edge.Source);
        TryAddNode(targetType, edge.Target);
    }

    /// <summary>
    /// Use this overload if type T is known at compile time.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /*public List<T>? GetEdges<T>() where T : IEdge<INode, INode>
    {
        return GetEdges<T>(Utilities.TypeToString<T>());
    }*/
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

        Utilities.CsvSerialize(
            x.NodeFeatures,
            Path.Join(outputDir, nodeFeaturesFilename),
            x.NodeFeaturesHeader);

        Utilities.CsvSerialize(
            x.EdgeFeatures,
            Path.Join(outputDir, edgeFeaturesFilename),
            x.EdgeFeaturesHeader);

        Utilities.CsvSerialize(
            x.PairIndices,
            Path.Join(outputDir, pairIndicesFilename),
            x.PairIndicesHeader);

        Utilities.CsvSerialize(
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



























    public int NodeCountV1 { get { return _nodesV1.Count; } }
    public int EdgeCountV1 { get { return _edgesV1.Count; } }

    /*public ReadOnlyCollection<ScriptNode> NodesV1
    {
        get { return new ReadOnlyCollection<ScriptNode>(_nodesV1.Values.ToList()); }
    }*/
    /*public ReadOnlyCollection<S2SEdge> EdgesV1
    {
        get { return new ReadOnlyCollection<S2SEdge>(_edgesV1.Values.ToList()); }
    }*/

    private readonly ConcurrentDictionary<string, ScriptNode> _nodesV1 = new();
    private readonly ConcurrentDictionary<string, S2SEdge> _edgesV1 = new();


    public ReadOnlyCollection<T2TEdge> T2TEdges
    {
        get { return new ReadOnlyCollection<T2TEdge>(_t2tEdges.Values.ToList()); }
    }
    private readonly ConcurrentDictionary<string, T2TEdge> _t2tEdges = new();
    /*
    public void AddNodeV1(ScriptNode node)
    {
        _nodesV1.AddOrUpdate(node.Id, node, (key, oldValue) => node);
        // TODO: any better update logic?!
    }*/
    /*
    public void AddNodeV1(Neo4j.Driver.INode node)
    {
        AddNodeV1(new ScriptNode(node));
    }*/
    /*
    public void AddNodesV1(IEnumerable<Neo4j.Driver.INode> nodes)
    {
        foreach (var node in nodes)
            AddNodeV1(new ScriptNode(node));
    }*/
    /*
    public void AddEdgeV1(IRelationship relationship)
    {
        var source = _nodesV1.GetOrAdd(
            relationship.StartNodeElementId,
            new ScriptNode(relationship.StartNodeElementId));

        var target = _nodesV1.GetOrAdd(
            relationship.EndNodeElementId,
            new ScriptNode(relationship.EndNodeElementId));

        var cEdge = new S2SEdge(source, target, relationship);
        var edge = _edgesV1.GetOrAdd(cEdge.Id, cEdge);

        source.AddOutgoingEdges(edge);
        target.AddIncomingEdges(edge);
    }*/
    /*
    public void AddEdgesV1(IEnumerable<IRelationship> edges)
    {
        foreach (var edge in edges)
            AddEdgeV1(edge);
    }*/
    /*
    public void AddEdgeV1(T2TEdge edge)
    {
        // TODO: ID may not be correct
        // TODO: tryadd is not the correct call

        _t2tEdges.TryAdd(edge.Id, edge);
    }*/
}
