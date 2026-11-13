namespace AAB.EBA.CLI.Config;

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

    public string BatchesFilename { init; get; } = "batches.json";

    public bool CompressOutput { init; get; } = true;

    /// <summary>
    /// increasing this will result in fewer and larger batches, 
    /// generally no issue with neo4j admin.
    /// But given SetTxoSpentHeight reads each batch into memory, 
    /// you should keep this low enough to avoid running out of memory.
    /// You can assume: 
    ///     - 1M entries: about 200–250 MB
    ///     - 5M entries: about 1–1.3 GB
    /// </summary>
    public int MaxEntitiesPerBatch { init; get; } = 2_000_000;
}
