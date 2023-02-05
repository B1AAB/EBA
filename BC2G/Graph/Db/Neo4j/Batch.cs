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

    public ImmutableDictionary<string, TypeInfo> TypesInfo
    {
        set { _typesInfo = new Dictionary<string, TypeInfo>(value); }
        get { return _typesInfo.ToImmutableDictionary(); }
    }
    private Dictionary<string, TypeInfo> _typesInfo = new();

    public BatchInfo() { }

    public void AddType(Type type, int count, string directory)
    {
        var key = GetKey(type);
        if (!_typesInfo.ContainsKey(key))
        {
            _typesInfo.Add(key, new TypeInfo(
                Path.Join(directory, $"{type.GUID:N}_{DateTime.Now:yyyyMMddHHmmssffff}.csv"),
                0));
        }

        _typesInfo[key].Count += count;
    }

    public string GetFilename(Type type)
    {
        var key = GetKey(type);
        if (!_typesInfo.ContainsKey(key))
            return string.Empty;
        else
            return _typesInfo[key].Filename;
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
