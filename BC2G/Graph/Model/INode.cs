namespace BC2G.Graph.Model;

public interface INode : IGraphComponent
{
    public string Id { get; }
    public int InDegree { get; }
    public int OutDegree { get; }

    public List<IEdge<INode, INode>> IncomingEdges { get; }
    public List<IEdge<INode, INode>> OutgoingEdges { get; }

    public double[] GetFeatures();

    /// <summary>
    /// this can return ID, or any unique label (e.g., script address, or tx hash).
    /// The goal of this method is to return unique label that would be more intuitive 
    /// for the user than ID (such as Neo4j ID). 
    /// </summary>
    /// <returns></returns>
    public string GetUniqueLabel();

    public void AddIncomingEdge(IEdge<INode, INode> incomingEdge);
    public void AddOutgoingEdge(IEdge<INode, INode> outgoingEdge);

    public void AddIncomingEdges(List<IEdge<INode, INode>> incomingEdges);
    public void AddOutgoingEdges(List<IEdge<INode, INode>> outgoingEdges);
}
