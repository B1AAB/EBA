using EBA.Utilities;

namespace EBA.Graph.Db.Neo4jDb;

public class Batch
{
    public string Name { get; }
    public string DefaultDirectory { get; }
    private readonly bool _compressOutput;

    public IReadOnlyDictionary<Type, TypeInfo> Types => _typesInfo;
    private readonly Dictionary<Type, TypeInfo> _typesInfo;

    [JsonConstructor]
    public Batch(
        string name,
        string defaultDirectory,
        IDictionary<Type, TypeInfo> typesInfo)
    {
        Name = name;
        DefaultDirectory = defaultDirectory;
        _typesInfo = new Dictionary<Type, TypeInfo>(typesInfo);
    }

    public Batch(string name, string defaultDirectory, IReadOnlyDictionary<Type, StrategyBase> strategies, bool compresseOutput)
    {
        Name = name;
        DefaultDirectory = defaultDirectory;
        _compressOutput = compresseOutput;
        var timestamp = Helpers.GetUnixTimeSeconds();

        _typesInfo = [];
        foreach (var strategy in strategies)
            _typesInfo.Add(
                strategy.Key,
                new TypeInfo(
                    Path.Join(DefaultDirectory, $"{timestamp}_{strategy.Value.DefaultFilename}"),
                    0));
    }

    public void Update(Type type, int count)
    {
        _typesInfo[type].Count += count;
    }

    public string GetFilename(Type type)
    {
        return _typesInfo[type].Filename;
    }

    public int GetMaxCount()
    {
        return (from x in _typesInfo.Values select x.Count).Max();
    }
}
