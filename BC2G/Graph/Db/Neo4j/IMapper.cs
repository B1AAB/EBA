using INode = BC2G.Graph.Model.INode;

namespace BC2G.Graph.Db.Neo4j;

public interface IMapperBase
{
    public string GetCsvHeader();

    /// <summary>
    /// The order of executing (de)serialization of 
    /// types is not fixed, hence, the queries should 
    /// be stateless and do not assume any precedence 
    /// on existing types. For instance, a query creating
    /// edges, should not assume nodes related to blocks
    /// exist.
    /// </summary>
    public string GetQuery(string filename);
}
public interface IEdgeMapper : IMapperBase
{
    public string GetCsv(IEdge<INode, INode> edge);

    public void ToCsv(IEnumerable<IEdge<INode, INode>> edges, string filename);
}

public interface IGraphMapper : IMapperBase
{
    public string GetCsv(GraphBase graph);
    public void ToCsv(GraphBase graph, string filename);
}
