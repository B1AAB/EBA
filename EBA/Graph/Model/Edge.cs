using EBA.Graph.Bitcoin.Strategies;
using EBA.Utilities;

namespace EBA.Graph.Model;

public class Edge<TSource, TTarget> : IEdge<TSource, TTarget>, IEquatable<Edge<TSource, TTarget>>
    where TSource : notnull, INode
    where TTarget : notnull, INode
{
    public static EdgeKind Kind { get { throw new NotImplementedException(); } }
    public EdgeKind EdgeKind { get; }

    public string Id { get; }
    public TSource Source { get; }
    public TTarget Target { get; }
    public long Value { get; }
    public RelationType Relation { get; }
    public uint Timestamp { get; }
    public long BlockHeight { get; }

    public static string Header
    {
        get
        {
            return string.Join(_delimiter, new string[]
            {
                "Source",
                "Target",
                "Value",
                "EdgeType",
                "TimeOffsetFromGenesisBlock",
                "BlockHeight"
            });
        }
    }

    private const string _delimiter = "\t";

    public Edge(
        TSource source, TTarget target,
        long value, RelationType relation,
        uint timestamp, long blockHeight)
    {
        Source = source;
        Target = target;
        Value = value;
        Relation = relation;
        Timestamp = timestamp;
        BlockHeight = blockHeight;

        Id = GetHashCode().ToString();

        EdgeKind = new EdgeKind(source.NodeKind, target.NodeKind, relation);
    }

    public Edge(
        TSource source, TTarget target,
        IRelationship relationship)
    {
        Source = source;
        Target = target;
        Id = relationship.ElementId;
        Value = Helpers.BTC2Satoshi(PropertyMappingFactory.ValueBTC<IRelationship>(null!).Deserialize<double>(relationship.Properties));
        Relation = Enum.Parse<RelationType>(relationship.Type);
        BlockHeight = PropertyMappingFactory.Height<IRelationship>(null!).Deserialize<long>(relationship.Properties);
    }

    public static string[] GetFeaturesName()
    {
        return [
            nameof(Value),
            nameof(Relation),
            nameof(BlockHeight) ];
    }

    public virtual double[] GetFeatures()
    {
        return
        [
            Helpers.Satoshi2BTC(Value),
            (double)Relation,
            BlockHeight
        ];
    }

    public void AddValue(long value)
    {
        // TODO: check when this can happen
        //throw new NotImplementedException();
    }

    public string GetHashCode(bool ignoreValue)
    {
        if (ignoreValue)
            return HashCode.Combine(Source.Id, Target.Id, Relation, Timestamp).ToString();
        else
            return GetHashCode().ToString();
    }

    public int GetHashCodeInt(bool ignoreValue)
    {
        if (ignoreValue)
            return HashCode.Combine(Source.Id, Target.Id, Relation, Timestamp);
        else
            return GetHashCode();
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Source.Id, Target.Id, Value, Relation, Timestamp);
    }

    public bool Equals(Edge<TSource, TTarget>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Source.Id == other.Source.Id
            && Target.Id == other.Target.Id
            && Value == other.Value
            && Relation == other.Relation
            && Timestamp == other.Timestamp;
    }
}
