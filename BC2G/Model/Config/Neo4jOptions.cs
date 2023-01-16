namespace BC2G.Model.Config
{
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
            @"E:\neo4j\relate-data\dbmss\dbms-c69cf826-4c55-4600-bd25-49113cbdde6e\import";

        public string CypherImportPrefix { set; get; } =
            Environment.GetEnvironmentVariable("NEO4J_CYPHERIMPORTPREFIX") ??
            "file:///";
    }
}
