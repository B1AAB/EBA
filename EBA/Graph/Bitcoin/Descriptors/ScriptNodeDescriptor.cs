namespace EBA.Graph.Bitcoin.Descriptors;

public class ScriptNodeDescriptor : IElementDescriptor<ScriptNode>
{
    public static string IdSpace => _idSpace;
    private readonly static string _idSpace = ScriptNode.Kind.ToString();

    public ElementMapper<ScriptNode> Mapper => StaticMapper;
    public static ElementMapper<ScriptNode> StaticMapper => _mapper;
    private static readonly ElementMapper<ScriptNode> _mapper = new(
        new MappingBuilder<ScriptNode>()
            .Map(n => n.SHA256Hash).WithCsvHeader(p => p.GetIdFieldCsvHeader(_idSpace))
            .Map(n => n.Address)
            .Map(n => n.ScriptType)
            .Map(n => n.HexBase64)
            .MapLabel(_ => ScriptNode.Kind)
            .ToArray());

    public static ScriptNode Deserialize(
        IReadOnlyDictionary<string, object> props,
        double? originalIndegree,
        double? originalOutdegree,
        double? hopsFromRoot,
        string? idInGraphDb)
    {
        return new ScriptNode(
            address: _mapper.GetValue(n => n.Address, props),
            scriptType: _mapper.GetValue(n => n.ScriptType, props),
            sha256Hash: _mapper.GetValue(n => n.SHA256Hash, props)!,
            hexBase64: _mapper.GetValue(n => n.HexBase64, props),
            originalIndegree: originalIndegree,
            originalOutdegree: originalOutdegree,
            hopsFromRoot: hopsFromRoot,
            idInGraphDb: idInGraphDb);
    }

    public string[] Neo4jSchemaOverride
    {
        get
        {
            return
            [
                $"CREATE CONSTRAINT {ScriptNode.Kind}_{nameof(ScriptNode.SHA256Hash)}_Unique " +
                $"IF NOT EXISTS " +
                $"FOR (v:{ScriptNode.Kind}) REQUIRE v.{nameof(ScriptNode.SHA256Hash)} IS UNIQUE"
            ];
        }
    }
}