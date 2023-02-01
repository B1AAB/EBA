namespace BC2G.Graph.Db;

public interface IGraphDb<T> : IDisposable where T : GraphBase
{
    public void Serialize(T graph);
    public void Import();
}
