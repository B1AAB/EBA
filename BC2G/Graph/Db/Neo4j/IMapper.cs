using INode = BC2G.Graph.Model.INode;

namespace BC2G.Graph.Db.Neo4j;

public interface IMapper
{
    public string GetCsv(IEdge<INode, INode> entity);
    public string GetCsvHeader();
    public string GetQuery(string csvFilename);
}
