namespace EBA.Graph.Bitcoin.Descriptors;

public class T2SEdgeDescriptor : IElementDescriptor<T2SEdge>
{
    public static string IdSpace => T2SEdge.Kind.ToString();

    public ElementMapper<T2SEdge> Mapper => StaticMapper;
    public static ElementMapper<T2SEdge> StaticMapper => _mapper;
    private static readonly ElementMapper<T2SEdge> _mapper = new(
        new MappingBuilder<T2SEdge>()
            .MapSourceId(TxNodeDescriptor.IdSpace, e => e.Source.Txid)
            .MapTargetId(ScriptNodeDescriptor.IdSpace, e => e.Target.Id)
            .Map(e => e.Value)
            .Map(e => e.Vout)
            .Map(e => e.CreationHeight)
            .Map(e => e.SpentHeight)
            .MapEdgeType(e => e.Relation)
            .ToArray());

    public static T2SEdge Deserialize(
        TxNode source, 
        ScriptNode target, 
        IReadOnlyDictionary<string, object> props)
    {
        return new T2SEdge(
            source: source,
            target: target,
            timestamp: 0,
            creationHeight: _mapper.GetValue(n => n.CreationHeight, props),
            value: _mapper.GetValue(n => n.Value, props),
            outputIndex: _mapper.GetValue(n => n.Vout, props),
            spentHeight: _mapper.GetValue(n => n.SpentHeight, props));
    }

    public string[]? Neo4jSchemaOverride
    {
        get
        {
            return
            [
                $"CREATE INDEX utxo_spending_idx IF NOT EXISTS " +
                $"\r\nFOR ()-[r:{T2SEdge.Kind.Relation}]-() " +
                $"\r\nON (r.{nameof(T2SEdge.CreationHeight)}, r.{nameof(T2SEdge.SpentHeight)})"
            ];
        }
    }
}