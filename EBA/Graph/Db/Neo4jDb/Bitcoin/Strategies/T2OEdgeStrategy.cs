using EBA.Utilities;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class T2OEdgeStrategy(bool serializeCompressed) : BitcoinEdgeStrategy(serializeCompressed)
{
    private static readonly PropertyMapping<T2OEdge>[] _mappings =
    [
        PropertyMappingFactory.SourceId<T2OEdge>(TxNodeStrategy.Label, e => e.Source.Txid),
        PropertyMappingFactory.TargetId<T2OEdge>(NonStandardNodeStrategy.Label, e => e.Target.Id),
        PropertyMappingFactory.ValueBTC<T2OEdge>(e => Helpers.Satoshi2BTC(e.Value)),
        PropertyMappingFactory.Height<T2OEdge>(e => e.BlockHeight),
        PropertyMappingFactory.EdgeType<T2OEdge>(e => e.Type)
    ];

    public override string GetCsvHeader()
    {
        return _mappings.GetCsvHeader();
    }

    public override string GetCsvRow(IGraphComponent component)
    {
        return GetCsv((T2OEdge)component);
    }

    public static string GetCsv(T2OEdge edge)
    {
        return _mappings.GetCsv(edge);
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
    }
}
