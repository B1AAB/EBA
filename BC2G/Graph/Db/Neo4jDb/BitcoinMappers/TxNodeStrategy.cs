namespace BC2G.Graph.Db.Neo4jDb.BitcoinMappers;

public class TxNodeStrategy : NodeStrategyBase
{
    public const string labels = "Tx";

    private readonly Property[] _properties = new Property[]
    {
        Props.Txid,
        Props.TxVersion,
        Props.TxSize,
        Props.TxVSize,
        Props.TxWeight,
        Props.TxLockTime
    };

    public override string GetCsvHeader()
    {
        return string.Join(
            csvDelimiter,
            from x in _properties select x.CsvHeader);
    }

    public override string GetCsv(Model.INode node)
    {
        return GetCsv((TxNode)node);
    }

    public static string GetCsv(TxNode node)
    {
        return string.Join(
            csvDelimiter,
            node.Txid,
            node.Version,
            node.Size,
            node.VSize,
            node.Weight,
            node.LockTime);
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
    }
}
