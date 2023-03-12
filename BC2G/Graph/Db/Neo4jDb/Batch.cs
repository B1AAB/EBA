using System.Collections.Immutable;

namespace BC2G.Graph.Db.Neo4jDb;

public class Batch
{
    public string Name { get; }
    public string DefaultDirectory { get; }

    public ImmutableDictionary<GraphComponentType, TypeInfo> TypesInfo
    {
        get { return _typesInfo.ToImmutableDictionary(); }
    }
    private readonly Dictionary<GraphComponentType, TypeInfo> _typesInfo;


    [JsonConstructor]
    public Batch(
        string name,
        string defaultDirectory,
        ImmutableDictionary<GraphComponentType, TypeInfo> typesInfo)
    {
        Name = name;
        DefaultDirectory = defaultDirectory;
        _typesInfo = new Dictionary<GraphComponentType, TypeInfo>(typesInfo);
    }

    public Batch(string name, string defaultDirectory, List<GraphComponentType> types)
    {
        Name = name;
        DefaultDirectory = defaultDirectory;
        var timestamp = Utilities.GetTimestamp();

        _typesInfo = new();
        foreach (var type in types)
            _typesInfo.Add(type, new TypeInfo(
                CreateFilename(type, timestamp, DefaultDirectory), 0));
    }

    public void AddOrUpdate(GraphComponentType type, int count)
    {
        AddOrUpdate(type, count, DefaultDirectory);
    }

    public void AddOrUpdate(GraphComponentType type, int count, string directory)
    {
        EnsureType(type, directory);
        _typesInfo[type].Count += count;
    }

    public string GetFilename(GraphComponentType type)
    {
        EnsureType(type, DefaultDirectory);
        return _typesInfo[type].Filename;
    }

    public int GetTotalCount()
    {
        return (from x in _typesInfo.Values select x.Count).Sum();
    }

    public TypeInfo GetTypeInfo(GraphComponentType type)
    {
        return _typesInfo[type];
    }

    private void EnsureType(GraphComponentType type, string directory)
    {
        if (!_typesInfo.ContainsKey(type))
        {
            _typesInfo.Add(type, new TypeInfo(
                CreateFilename(type, Utilities.GetTimestamp(), directory), 0));
        }
    }

    private static string CreateFilename(GraphComponentType type, string timestamp, string directory)
    {
        return Path.Join(directory, $"{timestamp}_{type}.csv");
    }
}
