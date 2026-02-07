using EBA.Utilities;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class T2NEdgeStrategy(bool serializeCompressed) : BitcoinEdgeStrategy(serializeCompressed)
{
    private static readonly PropertyMapping<T2NEdge>[] _mappings =
    [
        PropertyMappingFactory.SourceId<T2NEdge>(TxNodeStrategy.Label, e => e.Source.Txid),
        PropertyMappingFactory.TargetId<T2NEdge>(NullDataNodeStrategy.Label, e => e.Target.Id),
        PropertyMappingFactory.ValueBTC<T2NEdge>(e => Helpers.Satoshi2BTC(e.Value)),
        PropertyMappingFactory.Height<T2NEdge>(e => e.BlockHeight),
        PropertyMappingFactory.EdgeType<T2NEdge>(e => e.Type)
    ];

    public override string GetCsvHeader()
    {
        return _mappings.GetCsvHeader();
    }

    public override string GetCsvRow(IGraphComponent component)
    {
        return GetCsv((T2NEdge)component);
    }

    public static string GetCsv(T2NEdge edge)
    {
        return _mappings.GetCsv(edge);
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
    }
}
