namespace BC2G.Graph.Model;

public interface INode : IGraphComponent
{
    public string Id { get; }
    public int InDegree { get; }
    public int OutDegree { get; }

    public List<IEdge<INode, INode>> IncomingEdges { get; }
    public List<IEdge<INode, INode>> OutgoingEdges { get; }

    public double[] GetFeatures();

    public void AddIncomingEdge(IEdge<INode, INode> incomingEdge);
    public void AddOutgoingEdge(IEdge<INode, INode> outgoingEdge);

    public void AddIncomingEdges(List<IEdge<INode, INode>> incomingEdges);
    public void AddOutgoingEdges(List<IEdge<INode, INode>> outgoingEdges);
}
