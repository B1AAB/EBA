using EBA.Utilities;

namespace EBA.Graph.Db.Neo4jDb;

public class Batch
{
    public string Name { get; }
    public string DefaultDirectory { get; }
    private readonly bool _compressOutput;

    public ReadOnlyDictionary<string, TypeInfo> TypesInfo => new(_typesInfo);
    private readonly Dictionary<string, TypeInfo> _typesInfo;

    [JsonConstructor]
    public Batch(
        string name,
        string defaultDirectory,
        IDictionary<string, TypeInfo> typesInfo)
    {
        Name = name;
        DefaultDirectory = defaultDirectory;
        _typesInfo = [];
    }

    public Batch(
        string name,
        string defaultDirectory,
        IReadOnlyDictionary<NodeKind, StrategyBase> nodeStrategies,
        IReadOnlyDictionary<EdgeKind, StrategyBase> edgeStrategies,
        bool compresseOutput)
    {
        Name = name;
        DefaultDirectory = defaultDirectory;
        _compressOutput = compresseOutput;
        var timestamp = Helpers.GetUnixTimeSeconds();

        _typesInfo = [];
        foreach (var strategy in nodeStrategies)
        {
            _typesInfo.Add(
                strategy.Key.ToString(),
                new TypeInfo(
                    Path.Join(DefaultDirectory, $"{timestamp}_{strategy.Value.DefaultFilename}"),
                    0));
        }

        foreach (var strategy in edgeStrategies)
        {
            _typesInfo.Add(
                strategy.Key.ToString(),
                new TypeInfo(
                    Path.Join(DefaultDirectory, $"{timestamp}_{strategy.Value.DefaultFilename}"),
                    0));
        }
    }

    public void Update(NodeKind kind, int count)
    {
        _typesInfo[kind.ToString()].Count += count;
    }

    public void Update(EdgeKind kind, int count)
    {
        _typesInfo[kind.ToString()].Count += count;
    }

    public string GetFilename(NodeKind kind)
    {
        return _typesInfo[kind.ToString()].Filename;
    }

    public string GetFilename(EdgeKind kind)
    {
        return _typesInfo[kind.ToString()].Filename;
    }

    public int GetMaxCount()
    {
        return (from x in _typesInfo.Values select x.Count).Max();
    }
}
