namespace BC2G.Graph.Db.Neo4jDb;

public interface IGraphStrategy : IStrategyBase
{
    public string GetCsv(GraphBase graph);
    public void ToCsv(GraphBase graph, string filename);
}
