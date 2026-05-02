namespace AAB.EBA.Graph.Bitcoin.Descriptors;

public class C2TEdgeDescriptor : IElementDescriptor<C2TEdge>
{
    public static string IdSpace => _idSpace;
    private static readonly string _idSpace = C2TEdge.Kind.ToString();

    public ElementMapper<C2TEdge> Mapper => StaticMapper;
    public static ElementMapper<C2TEdge> StaticMapper => _mapper;
    private static readonly ElementMapper<C2TEdge> _mapper = new(
        new MappingBuilder<C2TEdge>()
            .MapSourceId(CoinbaseNode.Kind.ToString(), _ => CoinbaseNode.Kind)
            .MapTargetId(TxNodeDescriptor.IdSpace, e => e.Target.Txid)
            .Map(e => e.Value)
            .Map(e => e.Height)
            .MapEdgeType(e => e.Relation)
            .ToArray());

    public static C2TEdge Deserialize(
        TxNode target, 
        IReadOnlyDictionary<string, object> props)
    {
        return new C2TEdge(
            target: target,
            value: _mapper.GetValue(e => e.Value, props),
            timestamp: 0,
            height: _mapper.GetValue(e => e.Height, props));
    }
}