namespace BC2G.Graph.Model;

public interface IEdge<out TSource, out TTarget> : IGraphComponent
    where TSource : INode
    where TTarget : INode
{
    public string Id { get; }
    public TSource Source { get; }
    public TTarget Target { get; }
    public EdgeType Type { get; }
    public double Value { get; }

    public double[] GetFeatures();
    public string GetHashCode(bool ignoreValue);
    public int GetHashCodeInt(bool ignoreValue);
}
