namespace BC2G.Graph.Db.Neo4jDb;

public abstract class NodeStrategyBase : INodeStrategy
{
    public abstract string GetCsvHeader();
    public abstract string GetCsv(Model.INode node);
    public abstract string GetQuery(string filename);

    public void ToCsv(IEnumerable<Model.INode> nodes, string filename)
    {
        using var writer = new StreamWriter(filename, append: true);
        if (new FileInfo(filename).Length == 0)
            writer.WriteLine(GetCsvHeader());

        foreach (var node in nodes)
            writer.WriteLine(GetCsv(node));
    }
}
