namespace BC2G.Graph.Model;

public interface IEdge<out TSource, out TTarget, TNodeContext> : IGraphComponent
    where TSource : Model.INode<TNodeContext>
    where TTarget : Model.INode<TNodeContext>
    where TNodeContext: IContext
{
    public string Id { get; }
    public TSource Source { get; }
    public TTarget Target { get; }
    public EdgeType Type { get; }
    public long Value { get; }

    public double[] GetFeatures();
    public string GetHashCode(bool ignoreValue);
    public int GetHashCodeInt(bool ignoreValue);
}