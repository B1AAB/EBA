namespace BC2G.Graph.Db.Neo4j;

public interface IGraphMapper : IMapperBase
{
    public string GetCsv(GraphBase graph);
    public void ToCsv(GraphBase graph, string filename);
}
