using EBA.Utilities;

namespace EBA.Graph.Db.Neo4jDb;

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
        IReadOnlyDictionary<NodeKind, StrategyBase> nodeStrategies,
        IReadOnlyDictionary<EdgeKind, StrategyBase> edgeStrategies)
    {
        Name = name;
        DefaultDirectory = defaultDirectory;
        FilenamePrefix = Helpers.GetUnixTimeSeconds();

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
