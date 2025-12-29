namespace EBA.Graph.Model;

public class Node : INode
{
    public static GraphComponentType ComponentType
    {
        get { return GraphComponentType.Node; }
    }

    public virtual GraphComponentType GetGraphComponentType() { return ComponentType; }

    public string Id { get; }

    public string? IdInGraphDb { get; }

    public int InDegree { get { return IncomingEdges.Count; } }
    public int OutDegree { get { return OutgoingEdges.Count; } }

    /// <summary>
    /// This is the degree of the node in the entire graph 
    /// (e.g., the one in the database), where the Indegree and Outdegree 
    /// are the degrees in the graph where they are located (e.g., the sampled graph).
    /// </summary>
    public double? OriginalInDegree { get; }
    /// <summary>
    /// This is the degree of the node in the entire graph 
    /// (e.g., the one in the database), where the Indegree and Outdegree 
    /// are the degrees in the graph where they are located (e.g., the sampled graph).
    /// </summary>
    public double? OriginalOutDegree { get; }

    public double? OutHopsFromRoot { get; }

    public List<IEdge<INode, INode>> IncomingEdges { get; } = [];
    public List<IEdge<INode, INode>> OutgoingEdges { get; } = [];

    public static string Header
    {
        get
        {
            return "Id";
        }
    }

    public const char Delimiter = '\t';

    public Node(string id, double? originalInDegree = null, double? originalOutDegree = null, double? outHopsFromRoot = null, string? idInGraphDb = null)
    {
        Id = id;
        OriginalInDegree = originalInDegree;
        OriginalOutDegree = originalOutDegree;
        OutHopsFromRoot = outHopsFromRoot;
        IdInGraphDb = idInGraphDb;
    }

    public virtual string GetIdPropertyName()
    {
        return nameof(Id);
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
            nameof(OriginalInDegree),
            nameof(OriginalOutDegree),
            nameof(OutHopsFromRoot),
        ];
    }

    public virtual string[] GetFeatures()
    {
        return
        [
            InDegree.ToString(),
            OutDegree.ToString(),
            (OriginalInDegree == null ? double.NaN : (double)OriginalInDegree).ToString(),
            (OriginalOutDegree == null ? double.NaN :(double) OriginalOutDegree).ToString(),
            (OutHopsFromRoot == null ? double.NaN : (double) OutHopsFromRoot).ToString(),
        ];
    }

    public override string ToString()
    {
        return string.Join(Delimiter, [Id]);
    }
}
