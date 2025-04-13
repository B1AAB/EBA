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

    public Node(string id)
    {
        Id = id;
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
        return [nameof(InDegree), nameof(OutDegree)];
    }

    public virtual double[] GetFeatures()
    {
        return [InDegree, OutDegree];
    }

    public override string ToString()
    {
        return string.Join(Delimiter, [Id]);
    }
}
