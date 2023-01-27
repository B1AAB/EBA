namespace BC2G.Graph.Model;

public interface IEdge : IEdge<INode, INode>
{ }

public interface IEdge<out TSource, out TTarget>
    where TSource : INode
    where TTarget : INode
{
    public string Id { get; }
    public TSource Source { get; }
    public TTarget Target { get; }
    public EdgeType Type { get; }
    public double Value { get; }
}
