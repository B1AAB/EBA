using INode = BC2G.Graph.Model.INode;

namespace BC2G.Graph.Db.Neo4j.BitcoinMappers;

public abstract class BitcoinMapperBase : IMapper
{
    public const string csvDelimiter = "\t";
    public const string labelsDelimiter = ":";

    public static string CreatesEdgeQuery
    {
        get { return $"MERGE (block)-[:Creates {{{Props.Height.GetLoadExp(":")}}}]->(target) "; }
    }
    public static string RedeemsEdgeQuery
    {
        get { return $"MERGE (source)-[:Redeems {{{Props.Height.GetLoadExp(":")}}}]->(block) "; }
    }

    public abstract string GetCsv(IEdge<INode, INode> entity);
    public abstract string GetCsvHeader();
    public abstract string GetQuery(string csvFilename);
}
