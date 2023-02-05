using System.Collections.Immutable;

namespace BC2G.Graph.Db.Neo4j;

public class BatchInfo
{
    public class TypeInfo
    {
        public int Count { set; get; }
        public string Filename { get; }

        public TypeInfo(string filename, int count)
        {
            Filename = filename;
            Count = count;
        }
    }

    public string Name { set; get; } = string.Empty;

    public ImmutableDictionary<string, TypeInfo> TypesInfo
    {
        set { _typesInfo = new Dictionary<string, TypeInfo>(value); }
        get { return _typesInfo.ToImmutableDictionary(); }
    }
    private Dictionary<string, TypeInfo> _typesInfo = new();

    public BatchInfo() { }

    public void AddOrUpdate(string type, int count, string directory)
    {
        if (!_typesInfo.ContainsKey(type))
        {
            _typesInfo.Add(type, new TypeInfo(
                Path.Join(directory, $"{type}_{DateTime.Now:yyyyMMddHHmmssffff}.csv"),
                0));
        }

        _typesInfo[type].Count += count;
    }

    public string GetFilename(string type)
    {
        if (!_typesInfo.ContainsKey(type))
            return string.Empty;
        else
            return _typesInfo[type].Filename;
    }

    public int GetTotalCount()
    {
        return (from x in _typesInfo.Values select x.Count).Sum();
    }

    private static string GetKey(Type type)
    {
        return type.FullName ?? $"{type.Namespace ?? string.Empty}.{type.Name}";
    }
}
