namespace BC2G.Graph.Model;

public interface INode
{
    public string Id { get; }
    public int InDegree { get; }
    public int OutDegree { get; }

    public List<IEdge<INode, INode>> IncomingEdges { get; }
    public List<IEdge<INode, INode>> OutgoingEdges { get; }
}
