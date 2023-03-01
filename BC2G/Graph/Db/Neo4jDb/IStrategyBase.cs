namespace BC2G.Graph.Db.Neo4jDb;

public interface IStrategyBase
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
