namespace AAB.EBA.Graph.Db.Neo4jDb;

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
        "CREATE CONSTRAINT {0}_{1}_Unique IF NOT EXISTS FOR (n:{0}) REQUIRE n.{1} IS UNIQUE";

    public override string[] GetSchemaConfigs()
    {
        if (Descriptor.Neo4jSchemaOverride != null)
            return Descriptor.Neo4jSchemaOverride;
        
        if (Descriptor.UniqueProps.Length == 0)
            return [];

        var configs = new List<string>();
        if (!Descriptor.Mapper.TryGetMapping(":LABEL", out var mapping))
        {
            // Not currently supported for edge types,
            // because we're currently defining triplet type as instance property
            // (e.g., Transfer vs. Fee).
            // For all such constraints on edge types, use manual override. 
            return [];
        }

        var label = mapping!.GetValue(null!)?.ToString();

        foreach (var uniqueKey in Descriptor.UniqueProps)
            configs.Add(string.Format(
                _uniqueConstraintTemplate,
                label,
                uniqueKey));

        return [.. configs];
    }

    public override string[] GetSeedingCommands()
    {
        if (Descriptor.Neo4jSeedingOverride != null)
            return Descriptor.Neo4jSeedingOverride;

        return [];
    }
}
