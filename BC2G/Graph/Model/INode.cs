namespace BC2G.Graph.Model;

public interface INode<T> : IGraphComponent
    where T: IContext
{
    public string Id { get; }
    public int InDegree { get; }
    public int OutDegree { get; }

    /// <summary>
    /// This holds a context-specific state of a node, 
    /// which is a set of additional properties the 
    /// node can have given the context it is used in.
    /// 
    /// Implementation note: this implement policy pattern,
    /// the alternative is the decorator pattern where the 
    /// list of properties are held in a dictionary. 
    /// The downside of that is that the list of properties
    /// is unknow at compile time, which limits implementing
    /// property-specificc logic.
    /// </summary>
    public T Context { get; }

    public List<IEdge<INode<T>, INode<T>, T>> IncomingEdges { get; }
    public List<IEdge<INode<T>, INode<T>, T>> OutgoingEdges { get; }

    public string[] GetFeatures();

    /// <summary>
    /// this can return ID, or any unique label (e.g., script address, or tx hash).
    /// The goal of this method is to return unique label that would be more intuitive 
    /// for the user than ID (such as Neo4j ID). 
    /// </summary>
    /// <returns></returns>
    public string GetUniqueLabel();

    public void AddIncomingEdge(IEdge<INode<T>, INode<T>, T> incomingEdge);
    public void AddOutgoingEdge(IEdge<INode<T>, INode<T>, T> outgoingEdge);

    public void AddIncomingEdges(List<IEdge<INode<T>, INode<T>, T>> incomingEdges);
    public void AddOutgoingEdges(List<IEdge<INode<T>, INode<T>, T>> outgoingEdges);
}
