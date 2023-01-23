namespace BC2G.Graph.Db.Bulkload;

internal abstract class ModelMapper<T> : ModelMapper
{
    public ModelMapper(
    string workingDirectory,
    string cypherImportPrefix,
    //string importDirectory,
    string filename) : base(workingDirectory, cypherImportPrefix, filename)
    { }

    public abstract string ToCsv(T obj);
}

internal abstract class ModelMapper
{
    public const string csvDelimiter = "\t";
    public const string labelsDelimiter = ":";

    private const string _addressProperty = "Address";
    private const string _scriptTypeProperty = "ScriptType";

    public static Dictionary<Prop, Property> Props = new()
    {
        {Prop.Height, new Property("Height", FieldType.Int) },
        {Prop.ScriptAddress, new Property(_addressProperty) },
        {Prop.ScriptType, new Property(_scriptTypeProperty) },
        {Prop.BlockMedianTime, new Property("MedianTime")},
        {Prop.BlockConfirmations, new Property("Confirmations", FieldType.Int) },
        {Prop.BlockDifficulty, new Property("Difficulty" , FieldType.Float)},
        {Prop.BlockTxCount, new Property("TransactionsCount", FieldType.Int) },
        {Prop.BlockSize, new Property("Size", FieldType.Int) },
        {Prop.BlockStrippedSize, new Property("StrippedSize")},
        {Prop.BlockWeight, new Property("Weight", FieldType.Int) },
        {Prop.NumGenerationEdges, new Property("NumGenerationEdgeTypes", FieldType.Int) },
        {Prop.NumTransferEdges, new Property("NumTransferEdgeTypes", FieldType.Int) },
        {Prop.NumChangeEdges, new Property("NumChangeEdgeTypes", FieldType.Int) },
        {Prop.NumFeeEdges, new Property("NumFeeEdgeTypes", FieldType.Int) },
        {Prop.SumGenerationEdges, new Property("SumGenerationEdgeTypes", FieldType.Float) },
        {Prop.SumTransferEdges, new Property("SumTransferEdgeTypes", FieldType.Float) },
        {Prop.SumChangeEdges, new Property("SumChangeEdgeTypes", FieldType.Float) },
        {Prop.SumFeeEdges, new Property("SumFeeEdgeTypes", FieldType.Float) },
        {Prop.EdgeSourceAddress, new Property(_addressProperty, csvHeader: "SourceAddress") },
        {Prop.EdgeSourceType, new Property(_scriptTypeProperty, csvHeader: "SourceType") },
        {Prop.EdgeTargetAddress, new Property(_addressProperty, csvHeader: "DestAddress") },
        {Prop.EdgeTargetType, new Property(_scriptTypeProperty, csvHeader: "DestType") },
        {Prop.EdgeType, new Property("EdgeType") },
        {Prop.EdgeValue, new Property("Value", FieldType.Float) }
    };

    public static string CreatesEdgeQuery
    {
        get { return $"MERGE (block)-[:Creates {{{Props[Prop.Height].GetLoadExp(":")}}}]->(target) "; }
    }
    public static string RedeemsEdgeQuery
    {
        get { return $"MERGE (source)-[:Redeems {{{Props[Prop.Height].GetLoadExp(":")}}}]->(block) "; }
    }

    public string Batch { set; get; } = string.Empty;
    private readonly string _filename;
    public string AbsFilename { get { return Path.Combine(WorkingDirectory, Filename); } }
    public string Filename { get { return Batch + _filename; } }

    public string CypherQuery { get { return ComposeCypherQuery(CypherImportPrefix + Filename); } }

    public string CypherImportPrefix { get; }
    //public string ImportDir { get; }
    public string WorkingDirectory { get; }

    public ModelMapper(
        string workingDirectory,
        string cypherImportPrefix,
        //string importDirectory,
        string filename)
    {
        _filename = filename;
        //Filename = Path.Combine(importDirectory, filename);
        //ImportDir = importDirectory;
        WorkingDirectory = workingDirectory;
        CypherImportPrefix = cypherImportPrefix;
        //CypherQuery = ComposeCypherQuery(cypherImportPrefix + filename);
    }

    public abstract string GetCsvHeader();

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
