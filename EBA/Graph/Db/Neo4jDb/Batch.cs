using EBA.Utilities;

using System.Collections.Immutable;

namespace EBA.Graph.Db.Neo4jDb;

public class Batch
{
    public string Name { get; }
    public string DefaultDirectory { get; }
    private readonly bool _compressOutput;

    public ImmutableDictionary<Type, TypeInfo> TypesInfo
    {
        get { return _typesInfo.ToImmutableDictionary(); }
    }
    private readonly Dictionary<Type, TypeInfo> _typesInfo;


    [JsonConstructor]
    public Batch(
        string name,
        string defaultDirectory,
        ImmutableDictionary<Type, TypeInfo> typesInfo)
    {
        Name = name;
        DefaultDirectory = defaultDirectory;
        _typesInfo = new Dictionary<Type, TypeInfo>(typesInfo);
    }

    public Batch(string name, string defaultDirectory, List<Type> types, bool compresseOutput)
    {
        Name = name;
        DefaultDirectory = defaultDirectory;
        _compressOutput = compresseOutput;
        var timestamp = Helpers.GetUnixTimeSeconds();

        _typesInfo = [];
        foreach (var type in types)
            _typesInfo.Add(type, new TypeInfo(
                CreateFilename(type, timestamp, DefaultDirectory), 0));
    }

    public void AddOrUpdate(Type type, int count)
    {
        AddOrUpdate(type, count, DefaultDirectory);
    }

    public void AddOrUpdate(Type type, int count, string directory)
    {
        EnsureType(type, directory);
        _typesInfo[type].Count += count;
    }

    public string GetFilename(Type type)
    {
        EnsureType(type, DefaultDirectory);
        return _typesInfo[type].Filename;
    }

    public int GetMaxCount()
    {
        return (from x in _typesInfo.Values select x.Count).Max();
    }

    public TypeInfo GetTypeInfo(Type type)
    {
        return _typesInfo[type];
    }

    private void EnsureType(Type type, string directory)
    {
        if (!_typesInfo.ContainsKey(type))
        {
            _typesInfo.Add(type, new TypeInfo(
                CreateFilename(type, Helpers.GetUnixTimeSeconds(), directory), 0));
        }
    }

    private string CreateFilename(Type type, string timestamp, string directory)
    {
        return Path.Join(directory, $"{timestamp}_{type.Name}.csv{(_compressOutput == true ? ".gz" : "")}");
    }
}
