namespace AAB.EBA.Graph.Model;

public class Edge<TSource, TTarget> : IEdge<TSource, TTarget>, IEquatable<Edge<TSource, TTarget>>
    where TSource : notnull, INode
    where TTarget : notnull, INode
{
    public static EdgeKind Kind { get { throw new NotImplementedException($"Edge {nameof(Kind)} not defined"); } }
    public EdgeKind EdgeKind { get; }

    private static long _idCounter;

    public string Id { get; }
    public TSource Source { get; }
    public TTarget Target { get; }
    public long Value { get; }
    public RelationType Relation { get; }
    public long Height { get; }

    private const string _delimiter = "\t";

    public Edge(
        TSource source,
        TTarget target,
        RelationType relation,
        long value,
        long height)
    {
        Source = source;
        Target = target;
        Value = value;
        Relation = relation;
        Height = height;

        Id = Interlocked.Increment(ref _idCounter).ToString();

        EdgeKind = new EdgeKind(source.NodeKind, target.NodeKind, relation);
    }

    public static string[] GetFeaturesName()
    {
        return
        [
            nameof(Value),
            nameof(Relation),
            nameof(Height)
        ];
    }

    public virtual double[] GetFeatures()
    {
        return
        [
            Value,
            (double)Relation,
            Height
        ];
    }

    public void AddValue(long value)
    {
        // TODO: this method was called only a few times in ~400,000 blocks.
        // I was not able to reproduce it on the very same blocks it happened,
        // so it could be a race condition,
        // or more probably, a colission in the GetHashCode() method in the edges.
        throw new NotImplementedException("Edge.AddValue is not implemented.");
    }

    public override int GetHashCode()
    {
        // This method should include the same properties as the Equals() method.
        return HashCode.Combine(Source.Id, Target.Id, Value, Relation);
    }

    public bool Equals(Edge<TSource, TTarget>? other)
    {
        if (other is null) 
            return false;

        if (ReferenceEquals(this, other)) 
            return true;

        return Source.Id == other.Source.Id
            && Target.Id == other.Target.Id
            && Value == other.Value
            && Relation == other.Relation;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Edge<TSource, TTarget>);
    }
}
