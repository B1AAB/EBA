namespace BC2G.Graph.Db.Neo4jDb;

public interface INodeStrategy : IStrategyBase
{
    public string GetCsv(Model.INode node);

    public void ToCsv(IEnumerable<Model.INode> nodes, string filename);
}
