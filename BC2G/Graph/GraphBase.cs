namespace BC2G.Graph;

public class GraphBase : IEquatable<GraphBase>
{
    protected readonly ConcurrentDictionary<Node, double> _sources = new();
    protected readonly ConcurrentDictionary<Node, double> _targets = new();

    public List<Node> RewardsAddresses { set; get; } = new();

    public GraphBase()
    { }

    public bool Equals(GraphBase? other)
    {   
        if (other == null)
            return false;

        return ReferenceEquals(this, other);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as GraphBase);
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}

public class GraphBase2 : IEquatable<GraphBase2>
{
    public int NodeCount { get { return _nodes.Count; } }
    public int EdgeCount { get { return _edges.Count; } }

    public ReadOnlyCollection<Node> Nodes
    {
        get { return new ReadOnlyCollection<Node>(_nodes.Values.ToList()); }
    }
    public ReadOnlyCollection<Edge> Edges
    {
        get { return new ReadOnlyCollection<Edge>(_edges.Values.ToList()); }
    }
    public ReadOnlyCollection<double> Labels
    {
        get { return new ReadOnlyCollection<double>(_labels); }
    }


    private readonly ConcurrentDictionary<string, Node> _nodes = new();
    private readonly ConcurrentDictionary<string, Edge> _edges = new();
    private readonly List<double> _labels = new();

    public void AddNode(Node node)
    {
        _nodes.AddOrUpdate(node.Id, node, (key, oldValue) => node);
        // TODO: any better update logic?!
    }

    public void AddNode(INode node)
    {
        AddNode(new Node(node));
    }

    public void AddNodes(IEnumerable<INode> nodes)
    {
        foreach (var node in nodes)
            AddNode(new Node(node));
    }

    public void AddEdge(IRelationship relationship)
    {
        var source = _nodes.GetOrAdd(
            relationship.StartNodeElementId,
            new Node(relationship.StartNodeElementId));

        var target = _nodes.GetOrAdd(
            relationship.EndNodeElementId,
            new Node(relationship.EndNodeElementId));

        var cEdge = new Edge(source, target, relationship);
        var edge = _edges.GetOrAdd(cEdge.Id, cEdge);

        source.AddOutgoingEdges(edge);
        target.AddIncomingEdges(edge);
    }

    public void AddEdges(IEnumerable<IRelationship> edges)
    {
        foreach (var edge in edges)
            AddEdge(edge);
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

        Utilities.CsvSerialize(x.NodeFeatures, Path.Join(outputDir, nodeFeaturesFilename));
        Utilities.CsvSerialize(x.EdgeFeatures, Path.Join(outputDir, edgeFeaturesFilename));
        Utilities.CsvSerialize(x.PairIndices, Path.Join(outputDir, pairIndicesFilename));
        Utilities.CsvSerialize(x.Labels, Path.Combine(outputDir, labelsFilename));
    }

    public bool Equals(GraphBase2? other)
    {
        if (other == null)
            return false;

        return ReferenceEquals(this, other);
    }
    public bool Equals(object? other)
    {
        return Equals(other as GraphBase2);
    }
    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}
