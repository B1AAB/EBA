namespace AAB.EBA.Graph.Bitcoin.Descriptors;

public class T2TEdgeDescriptor : IElementDescriptor<T2TEdge>
{
    public static string IdSpace => T2TEdge.Kind.ToString();

    public ElementMapper<T2TEdge> Mapper => StaticMapper;
    public static ElementMapper<T2TEdge> StaticMapper => _mapper;
    private static readonly ElementMapper<T2TEdge> _mapper = new(
        new MappingBuilder<T2TEdge>()
            .MapSourceId(TxNodeDescriptor.IdSpace, e => e.Source.Txid)
            .MapTargetId(TxNodeDescriptor.IdSpace, e => e.Target.Txid)
            .Map(e => e.Value)
            .Map(e => e.Height)
            .MapEdgeType(e => e.Relation)
            .ToArray());

    public static T2TEdge Deserialize(
        TxNode source, 
        TxNode target, 
        IReadOnlyDictionary<string, object> props,
        string relationType)
    {
        return new T2TEdge(
            source: source,
            target: target,
            timestamp: 0,
            height: _mapper.GetValue(e => e.Height, props),
            value: _mapper.GetValue(e => e.Value, props),
            type: Enum.Parse<RelationType>(relationType, ignoreCase: true)); 
    }
}