namespace BC2G.Graph.Model;

public class Node : INode
{
    public static GraphComponentType ComponentType
    {
        get { return GraphComponentType.Node; }
    }

    public virtual GraphComponentType GetGraphComponentType() { return ComponentType; }

    public string Id { get; }

    public int InDegree { get { return IncomingEdges.Count; } }
    public int OutDegree { get { return OutgoingEdges.Count; } }

    /// <summary>
    /// This is the degree of the node in the entire graph 
    /// (e.g., the one in the database), where the Indegree and Outdegree 
    /// are the degrees in the graph where they are located (e.g., the sampled graph).
    /// </summary>
    public double? OriginalIndegree { get; }
    /// <summary>
    /// This is the degree of the node in the entire graph 
    /// (e.g., the one in the database), where the Indegree and Outdegree 
    /// are the degrees in the graph where they are located (e.g., the sampled graph).
    /// </summary>
    public double? OriginalOutdegree { get; }

    public List<IEdge<INode, INode>> IncomingEdges { get; } = [];
    public List<IEdge<INode, INode>> OutgoingEdges { get; } = [];

    public static string Header
    {
        get
        {
            return string.Join(Delimiter, new string[]
            {
                "Id",
            });
        }
    }

    public const char Delimiter = '\t';

    public Node(string id, double? originalIndegree = null, double? originalOutdegree = null)
    {
        Id = id;
        OriginalIndegree = originalIndegree;
        OriginalOutdegree = originalOutdegree;
    }

    public virtual string GetUniqueLabel()
    {
        return Id;
    }

    public void AddIncomingEdge(IEdge<INode, INode> incomingEdge)
    {
        IncomingEdges.Add(incomingEdge);
    }

    public void AddIncomingEdges(List<IEdge<INode, INode>> incomingEdges)
    {
        IncomingEdges.AddRange(incomingEdges);
    }

    public void AddOutgoingEdges(List<IEdge<INode, INode>> outgoingEdges)
    {
        OutgoingEdges.AddRange(outgoingEdges);
    }

    public void AddOutgoingEdge(IEdge<INode, INode> outgoingEdge)
    {
        OutgoingEdges.Add(outgoingEdge);
    }

    public static string[] GetFeaturesName()
    {
        return
        [
            nameof(InDegree),
            nameof(OutDegree),
            nameof(OriginalIndegree),
            nameof(OriginalOutdegree)
        ];
    }

    public virtual double[] GetFeatures()
    {
        return
        [
            InDegree,
            OutDegree,
            OriginalIndegree == null ? double.NaN : (double)OriginalIndegree,
            OriginalOutdegree == null ? double.NaN : (double)OriginalOutdegree
        ];
    }

    public override string ToString()
    {
        return string.Join(Delimiter, [Id]);
    }
}
