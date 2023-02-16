using INode = BC2G.Graph.Model.INode;

namespace BC2G.Graph.Db.Neo4j;

public interface IEdgeMapper : IMapperBase
{
    public string GetCsv(IEdge<INode, INode> edge);

    public void ToCsv(
        IEnumerable<IEdge<INode, INode>> edges, 
        string filename);
}
