using EBA.Graph.Bitcoin.Strategies;
using EBA.Utilities;

namespace EBA.Graph.Model;

public class Edge<TSource, TTarget> : IEdge<TSource, TTarget>
    where TSource : notnull, INode
    where TTarget : notnull, INode
{
    public string Id { get; }
    public EdgeKind Triplet { get; }
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

        Triplet = new EdgeKind(source.NodeKind, target.NodeKind, Relation);
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

    public void AddValue(long value)
    {
        throw new NotImplementedException();
    }
}
