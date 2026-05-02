namespace AAB.EBA.Graph.Bitcoin.Descriptors;

public class S2TEdgeDescriptor : IElementDescriptor<S2TEdge>
{
    public static string IdSpace => S2TEdge.Kind.ToString();

    public ElementMapper<S2TEdge> Mapper => StaticMapper;
    public static ElementMapper<S2TEdge> StaticMapper => _mapper;
    private static readonly ElementMapper<S2TEdge> _mapper = new(
        new MappingBuilder<S2TEdge>()
            .MapSourceId(ScriptNodeDescriptor.IdSpace, e => e.Source.Id)
            .MapTargetId(TxNodeDescriptor.IdSpace, e => e.Target.Txid)
            .Map(e => e.Value)
            .Map(e => e.SpentHeight)
            .Map(e => e.Txid)
            .Map(e => e.Vout)
            .Map(e => e.Generated)
            .Map(e => e.CreationHeight)
            .MapEdgeType(e => e.Relation)
            .ToArray());

    public static S2TEdge Deserialize(
        ScriptNode source, 
        TxNode target, 
        IReadOnlyDictionary<string, object> props)
    {
        return new S2TEdge(
            source: source,
            target: target,
            timestamp: 0,
            creationHeight: _mapper.GetValue(e => e.CreationHeight, props),
            spentHeight: _mapper.GetValue(e => e.SpentHeight, props),
            value: _mapper.GetValue(e => e.Value, props),
            txid: _mapper.GetValue(e => e.Txid, props),
            vout: _mapper.GetValue(e => e.Vout, props),
            generated: _mapper.GetValue(e => e.Generated, props)
        );
    }
}