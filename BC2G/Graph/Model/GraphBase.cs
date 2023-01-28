namespace BC2G.Graph.Model;

public class GraphBase : IEquatable<GraphBase>
{
    public int NodeCount { get { return _nodes.Count; } }
    public int EdgeCount { get { return _edges.Count; } }

    public ReadOnlyCollection<ScriptNode> Nodes
    {
        get { return new ReadOnlyCollection<ScriptNode>(_nodes.Values.ToList()); }
    }
    public ReadOnlyCollection<S2SEdge> Edges
    {
        get { return new ReadOnlyCollection<S2SEdge>(_edges.Values.ToList()); }
    }
    public ReadOnlyCollection<double> Labels
    {
        get { return new ReadOnlyCollection<double>(_labels); }
    }

    private readonly ConcurrentDictionary<string, ScriptNode> _nodes = new();
    private readonly ConcurrentDictionary<string, S2SEdge> _edges = new();
    private readonly List<double> _labels = new();

    public ReadOnlyCollection<T2TEdge> T2TEdges
    {
        get { return new ReadOnlyCollection<T2TEdge>(_t2tEdges.Values.ToList()); }
    }
    private readonly ConcurrentDictionary<string, T2TEdge> _t2tEdges = new();

    public void AddNode(ScriptNode node)
    {
        _nodes.AddOrUpdate(node.Id, node, (key, oldValue) => node);
        // TODO: any better update logic?!
    }

    public void AddNode(Neo4j.Driver.INode node)
    {
        AddNode(new ScriptNode(node));
    }

    public void AddNodes(IEnumerable<Neo4j.Driver.INode> nodes)
    {
        foreach (var node in nodes)
            AddNode(new ScriptNode(node));
    }

    public void AddEdge(IRelationship relationship)
    {
        var source = _nodes.GetOrAdd(
            relationship.StartNodeElementId,
            new ScriptNode(relationship.StartNodeElementId));

        var target = _nodes.GetOrAdd(
            relationship.EndNodeElementId,
            new ScriptNode(relationship.EndNodeElementId));

        var cEdge = new S2SEdge(source, target, relationship);
        var edge = _edges.GetOrAdd(cEdge.Id, cEdge);

        source.AddOutgoingEdges(edge);
        target.AddIncomingEdges(edge);
    }

    public void AddEdges(IEnumerable<IRelationship> edges)
    {
        foreach (var edge in edges)
            AddEdge(edge);
    }

    public void AddEdge(T2TEdge edge)
    {
        // TODO: ID may not be correct
        // TODO: tryadd is not the correct call

        _t2tEdges.TryAdd(edge.Id, edge);
    }

    public void AddLabel(double label)
    {
        _labels.Add(label);
    }

    public GraphFeatures GetFeatures()
    {
        return new GraphFeatures(this);
    }

    public void GetFeatures(
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
}
