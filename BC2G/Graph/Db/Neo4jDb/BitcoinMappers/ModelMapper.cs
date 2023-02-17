namespace BC2G.Graph.Db.Neo4jDb.BitcoinMappers;

internal abstract class ModelMapper<T>
{
    public const string csvDelimiter = "\t";
    public const string labelsDelimiter = ":";

    public static string CreatesEdgeQuery
    {
        get { return $"MERGE (block)-[:Creates {{{Props.Height.GetLoadExp(":")}}}]->(target) "; }
    }
    public static string RedeemsEdgeQuery
    {
        get { return $"MERGE (source)-[:Redeems {{{Props.Height.GetLoadExp(":")}}}]->(block) "; }
    }

    public string Batch { set; get; } = string.Empty;
    private readonly string _filename;
    public string AbsFilename { get { return Path.Combine(WorkingDirectory, Filename); } }
    public string Filename { get { return Batch + _filename; } }

    public string CypherQuery { get { return ComposeCypherQuery(CypherImportPrefix + Filename); } }

    public string CypherImportPrefix { get; }
    public string WorkingDirectory { get; }

    public ModelMapper(
        string workingDirectory,
        string cypherImportPrefix,
        string filename)
    {
        _filename = filename;
        WorkingDirectory = workingDirectory;
        CypherImportPrefix = cypherImportPrefix;
    }

    public abstract string GetCsvHeader();

    public abstract string ToCsv(T obj);

    protected abstract string ComposeCypherQuery(string filename);

    public bool TryParseFilename(string filename, out string batchName)
    {
        var pattern = new Regex(
            @"(?<batchName>\d{18})" + _filename,
            RegexOptions.Compiled,
            new TimeSpan(0, 0, 1));

        var match = pattern.Match(Path.GetFileName(filename));
        batchName = match.Groups["batchName"].Value;
        return match.Success;
    }
}
