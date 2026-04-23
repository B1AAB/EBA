namespace EBA.Graph.Bitcoin.Descriptors;

public class B2TEdgeDescriptor : IElementDescriptor<B2TEdge>
{
    public static string IdSpace => B2TEdge.Kind.ToString();

    public ElementMapper<B2TEdge> Mapper => StaticMapper;
    public static ElementMapper<B2TEdge> StaticMapper => _mapper;
    private static readonly ElementMapper<B2TEdge> _mapper = new(
        new MappingBuilder<B2TEdge>()
            .MapSourceId(BlockNodeDescriptor.IdSpace, e => e.Source.BlockMetadata.Height)
            .MapTargetId(TxNodeDescriptor.IdSpace, e => e.Target.Txid)
            .Map(e => e.Value)
            .Map(e => e.Height)
            .MapEdgeType(e => e.Relation)
            .ToArray());

    public static B2TEdge Deserialize(
        BlockNode source, 
        TxNode target, 
        IReadOnlyDictionary<string, object> props)
    {
        return new B2TEdge(
            source: source,
            target: target,
            timestamp: 0,
            height: _mapper.GetValue(e => e.Height, props),
            value: _mapper.GetValue(e => e.Value, props));
    }
}