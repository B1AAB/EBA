using BC2G.Utilities;

using System.Collections.Immutable;

namespace BC2G.Graph.Db.Neo4jDb;

public class Batch
{
    public string Name { get; }
    public string DefaultDirectory { get; }
    private bool _compressOutput;

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

    public Batch(string name, string defaultDirectory, List<GraphComponentType> types, bool compresseOutput)
    {
        Name = name;
        DefaultDirectory = defaultDirectory;
        _compressOutput = compresseOutput;
        var timestamp = Helpers.GetTimestamp();

        _typesInfo = [];
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

    public int GetMaxCount()
    {
        return (from x in _typesInfo.Values select x.Count).Max();
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
                CreateFilename(type, Helpers.GetTimestamp(), directory), 0));
        }
    }

    private string CreateFilename(GraphComponentType type, string timestamp, string directory)
    {
        return Path.Join(directory, $"{timestamp}_{type}.csv{(_compressOutput == true ? ".gz" : "")}");
    }
}
