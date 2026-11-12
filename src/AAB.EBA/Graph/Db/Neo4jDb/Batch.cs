using AAB.EBA.Utilities;

namespace AAB.EBA.Graph.Db.Neo4jDb;

public class Batch
{
    public string Name { get; }
    public string FilenamePrefix { get; }
    public string DefaultDirectory { get; }

    public Dictionary<string, TypeInfo> TypesInfo { get; }

    [JsonConstructor]
    public Batch(
        string name,
        string defaultDirectory,
        string filenamePrefix,
        Dictionary<string, TypeInfo> typesInfo)
    {
        Name = name;
        DefaultDirectory = defaultDirectory;
        FilenamePrefix = filenamePrefix;
        TypesInfo = new Dictionary<string, TypeInfo>(typesInfo);
    }

    public Batch(
        string name,
        string defaultDirectory,
        IReadOnlyDictionary<NodeKind, IElementCodec> nodeStrategies,
        IReadOnlyDictionary<EdgeKind, IElementCodec> edgeStrategies,
        Batch? previousBatch = null)
    {
        Name = name;
        DefaultDirectory = defaultDirectory;
        FilenamePrefix = GetUniqueFilenamePrefix(previousBatch);

        TypesInfo = [];
        foreach (var strategy in nodeStrategies)
        {
            TypesInfo.Add(
                strategy.Key.ToString(),
                new TypeInfo(
                    Path.Join(DefaultDirectory, $"{FilenamePrefix}_{strategy.Value.DefaultFilename}"),
                    0));
        }

        foreach (var strategy in edgeStrategies)
        {
            TypesInfo.Add(
                strategy.Key.ToString(),
                new TypeInfo(
                    Path.Join(DefaultDirectory, $"{FilenamePrefix}_{strategy.Value.DefaultFilename}"),
                    0));
        }
    }

    /// <summary>
    /// The prefix is a Unix timestamp in seconds. When a batch fills up
    /// in under a second (e.g., a block whose graph alone exceeds
    /// MaxEntitiesPerBatch), two consecutive batches would get the same
    /// prefix, hence the same filenames; both batches would then write to
    /// (and be read back from) the same files, so every element of those
    /// files appears in two batches. To avoid this, the prefix is forced
    /// to be strictly greater than the previous batch's prefix.
    /// </summary>
    private static string GetUniqueFilenamePrefix(Batch? previousBatch)
    {
        var prefix = long.Parse(Helpers.GetUnixTimeSeconds());
        if (previousBatch != null && long.TryParse(previousBatch.FilenamePrefix, out var previousPrefix))
            prefix = Math.Max(prefix, previousPrefix + 1);

        return prefix.ToString();
    }

    public void Update(NodeKind kind, int count)
    {
        TypesInfo[kind.ToString()].Count += count;
    }

    public void Update(EdgeKind kind, int count)
    {
        TypesInfo[kind.ToString()].Count += count;
    }

    public string GetFilename(NodeKind kind)
    {
        return TypesInfo[kind.ToString()].Filename;
    }

    public string GetFilename(EdgeKind kind)
    {
        return TypesInfo[kind.ToString()].Filename;
    }

    public int GetMaxCount()
    {
        return (from x in TypesInfo.Values select x.Count).Max();
    }

    public static async Task SerializeBatchesAsync(string filename, List<Batch> batches)
    {
        var json = JsonSerializer.Serialize(batches, Options.JsonSerializationOptions);
        await File.WriteAllTextAsync(filename, json);
    }

    public static async Task<List<Batch>> DeserializeBatchesAsync(string filename)
    {
        return await JsonSerializer<List<Batch>>.DeserializeAsync(filename);
    }
}
