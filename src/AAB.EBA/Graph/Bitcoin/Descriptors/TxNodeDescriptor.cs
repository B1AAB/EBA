namespace AAB.EBA.Graph.Bitcoin.Descriptors;

public class TxNodeDescriptor : IElementDescriptor<TxNode>
{
    public static string IdSpace => _idSpace;
    private static readonly string _idSpace = TxNode.Kind.ToString();

    public ElementMapper<TxNode> Mapper => StaticMapper;
    public static ElementMapper<TxNode> StaticMapper => _mapper;
    private static readonly ElementMapper<TxNode> _mapper = new(
        new MappingBuilder<TxNode>()
            .Map(n => n.Txid).WithCsvHeader(p => p.GetIdFieldCsvHeader(_idSpace))
            .Map(n => n.Version)
            .Map(n => n.Size)
            .Map(n => n.VSize)
            .Map(n => n.Weight)
            .Map(n => n.LockTime)
            .MapLabel(_ => TxNode.Kind)
            .ToArray());

    public static TxNode Deserialize(
        IReadOnlyDictionary<string, object> props,
        double? originalIndegree, 
        double? originalOutdegree, 
        double? hopsFromRoot,
        string? idInGraphDb)
    {
        return new TxNode(
            txid: _mapper.GetValue(n => n.Txid, props),
            version: _mapper.GetValue(n => n.Version, props),
            size: _mapper.GetValue(n => n.Size, props),
            vSize: _mapper.GetValue(n => n.VSize, props),
            weight: _mapper.GetValue(n => n.Weight, props),
            lockTime: _mapper.GetValue(n => n.LockTime, props),
            originalIndegree: originalIndegree,
            originalOutdegree: originalOutdegree,
            hopsFromRoot: hopsFromRoot,
            idInGraphDb: idInGraphDb);
    }

    public string[] UniqueProps =>
    [
        _mapper.GetMapping(x => x.Txid).Property.Name,
    ];
}