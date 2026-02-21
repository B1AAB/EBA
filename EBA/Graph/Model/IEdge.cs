using System.Runtime.CompilerServices;

namespace EBA.Graph.Model;

public interface IEdge<out TSource, out TTarget> : IGraphElement
    where TSource : INode
    where TTarget : INode
{
    public string Id { get; }
    public TSource Source { get; }
    public TTarget Target { get; }
    public RelationType Relation { get; }
    public EdgeKind EdgeKind { get; }
    public long Value { get; }

    public double[] GetFeatures();
    public string GetHashCode(bool ignoreValue);
    public int GetHashCodeInt(bool ignoreValue);

    public void AddValue(long value);
}
