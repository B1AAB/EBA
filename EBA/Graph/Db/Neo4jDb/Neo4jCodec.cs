namespace EBA.Graph.Db.Neo4jDb;

public class Neo4jCodec<T>(
    IElementDescriptor<T> descriptor,
    string defaultFilename,
    bool serializeCompressed) 
    : CodecBase<T>(
        descriptor, 
        defaultFilename, 
        serializeCompressed)
    where T : class, IGraphElement
{
    private const string _uniqueConstraintTemplate =
        "CREATE CONSTRAINT FOR (n:{0}) REQUIRE n.{1} IS UNIQUE";

    public override string[] GetSchemaConfigs()
    {
        if (Descriptor.Neo4jSchemaOverride != null)
            return Descriptor.Neo4jSchemaOverride;

        return
        [
            .. Descriptor
                .UniqueKeys
                .Select(key => string.Format(
                    _uniqueConstraintTemplate, 
                    Descriptor.Mapper.GetMapping(":LABEL").GetValue(null!)?.ToString(), 
                    key))
        ];
    }

    public override string[] GetSeedingCommands()
    {
        if (Descriptor.Neo4jSeedingOverride != null)
            return Descriptor.Neo4jSeedingOverride;

        return [];
    }
}
