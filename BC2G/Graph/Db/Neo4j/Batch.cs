using System.Collections.Immutable;

namespace BC2G.Graph.Db.Neo4j;

public class BatchInfo
{
    public string Name { get; }
    public string DefaultDirectory { get; }    
    
    public ImmutableDictionary<string, TypeInfo> TypesInfo
    {
        get { return _typesInfo.ToImmutableDictionary(); }
    }
    private readonly Dictionary<string, TypeInfo> _typesInfo;


    [JsonConstructor]
    public BatchInfo(
        string name, 
        string defaultDirectory, 
        ImmutableDictionary<string, TypeInfo> typesInfo)
    {
        Name = name;
        DefaultDirectory = defaultDirectory;
        _typesInfo = new Dictionary<string, TypeInfo>(typesInfo);
    }

    public BatchInfo(string name, string defaultDirectory, List<string> types)
    {
        Name = name;
        DefaultDirectory = defaultDirectory;
        var timestamp = GetTimestamp();

        _typesInfo = new();
        foreach (var type in types)
            _typesInfo.Add(type, new TypeInfo(
                CreateFilename(type, timestamp, DefaultDirectory), 0));
    }

    public void AddOrUpdate(string type, int count)
    {
        AddOrUpdate(type, count, DefaultDirectory);
    }

    public void AddOrUpdate(string type, int count, string directory)
    {
        EnsureType(type, directory);
        _typesInfo[type].Count += count;
    }

    public string GetFilename(string type)
    {
        EnsureType(type, DefaultDirectory);
        return _typesInfo[type].Filename;
    }

    public int GetTotalCount()
    {
        return (from x in _typesInfo.Values select x.Count).Sum();
    }

    public TypeInfo GetTypeInfo(string type)
    {
        return _typesInfo[type];
    }

    private void EnsureType(string type, string directory)
    {
        if (!_typesInfo.ContainsKey(type))
        {
            _typesInfo.Add(type, new TypeInfo(
                CreateFilename(type, GetTimestamp(), directory), 0));
        }
    }

    private static string GetTimestamp()
    {
        return $"{DateTime.Now:yyyyMMddHHmmssffff}";
    }
    private static string CreateFilename(string type, string timestamp, string directory)
    {
        return Path.Join(directory, $"{timestamp}_{type.Replace('.', '_')}.csv");
    }
}
