using EBA.Utilities;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class S2TEdgeStrategy(bool serializeCompressed) : BitcoinEdgeStrategy(serializeCompressed)
{
    private static readonly PropertyMapping<S2TEdge>[] _mappings =
    [
        PropertyMappingFactory.SourceId<S2TEdge>(ScriptNodeStrategy.Label, e => e.Source.Address),
        PropertyMappingFactory.TargetId<S2TEdge>(TxNodeStrategy.Label, e => e.Target.Txid),
        PropertyMappingFactory.ValueBTC<S2TEdge>(e => Helpers.Satoshi2BTC(e.Value)),
        PropertyMappingFactory.Height<S2TEdge>(e => e.BlockHeight),
        new(nameof(S2TEdge.UTxOCreatedInBlockHeight), FieldType.Long, e => e.UTxOCreatedInBlockHeight),
        PropertyMappingFactory.EdgeType<S2TEdge>(e => e.Type)
    ];

    public override string GetCsvHeader()
    {
        return _mappings.GetCsvHeader();
    }

    public override string GetCsvRow(IGraphComponent edge)
    {
        return GetCsvRow((S2TEdge)edge);
    }

    public static string GetCsvRow(S2TEdge edge)
    {
        return _mappings.GetCsv(edge);
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
    }
}