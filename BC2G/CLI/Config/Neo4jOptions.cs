namespace BC2G.CLI.Config;

public class Neo4jOptions
{
    public string Uri { get; set; } =
        Environment.GetEnvironmentVariable("NEO4J_URI") ??
        "bolt://localhost:7687";

    public string User { get; set; } =
        Environment.GetEnvironmentVariable("NEO4J_USER") ??
        "neo4j";

    public string Password { set; get; } =
        Environment.GetEnvironmentVariable("NEO4J_PASSWORD") ??
        "password";

    public string ImportDirectory { set; get; } =
        Environment.GetEnvironmentVariable("NEO4J_IMPORTDIRECTORY") ??
        @"E:\neo4j\relate-data\dbmss\dbms-fa9d2708-e8ab-478a-914c-6413e4b8c40f\import";

    public string BatchesFilename { set; get; } = "batches.json";

    public string CypherImportPrefix { set; get; } =
        Environment.GetEnvironmentVariable("NEO4J_CYPHERIMPORTPREFIX") ??
        "file:///";
}
