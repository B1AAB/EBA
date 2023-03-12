namespace BC2G.CLI.Config;

public class Neo4jOptions
{
    public string Uri { init; get; } =
        Environment.GetEnvironmentVariable("NEO4J_URI") ??
        "bolt://localhost:7687";

    public string User { init; get; } =
        Environment.GetEnvironmentVariable("NEO4J_USER") ??
        "neo4j";

    public string Password { init; get; } =
        Environment.GetEnvironmentVariable("NEO4J_PASSWORD") ??
        "password";

    public string ImportDirectory { init; get; } =
        Environment.GetEnvironmentVariable("NEO4J_IMPORTDIRECTORY") ??
        @"E:\neo4j\relate-data\dbmss\dbms-dfca261d-2476-4d4e-ac5e-ead99120ab3e\import";

    public string BatchesFilename { init; get; } = "batches.json";

    public string CypherImportPrefix { init; get; } =
        Environment.GetEnvironmentVariable("NEO4J_CYPHERIMPORTPREFIX") ??
        "file:///";
}
