namespace BC2G.Graph.Model;

public class Node : INode
{
    public string Id { get; }

    public int InDegree { get { return IncomingEdges.Count; } }
    public int OutDegree { get { return OutgoingEdges.Count; } }

    public List<IEdge<INode, INode>> IncomingEdges { get; } = new();
    public List<IEdge<INode, INode>> OutgoingEdges { get; } = new();

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

    public void AddIncomingEdges(IEdge<INode, INode> incomingEdge)
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

    public void AddOutgoingEdges(IEdge<INode, INode> outgoingEdge)
    {
        OutgoingEdges.Add(outgoingEdge);
    }

    public static string[] GetFeaturesName()
    {
        return new string[] { nameof(ScriptType), nameof(InDegree), nameof(OutDegree) };
    }

    public double[] GetFeatures()
    {
        return new double[] { InDegree, OutDegree };
    }

    public override string ToString()
    {
        return string.Join(
            Delimiter,
            new string[] { Id });
    }
}
