using EBA.Utilities;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class S2TEdgeStrategy(bool serializeCompressed) : BitcoinEdgeStrategy(serializeCompressed)
{
    private static readonly PropertyMapping<S2TEdge>[] _mappings =
    [
        MappingHelpers.SourceId<S2TEdge>(ScriptNodeStrategy.Label, e => e.Source.Address),
        MappingHelpers.TargetId<S2TEdge>(TxNodeStrategy.Label, e => e.Target.Txid),
        MappingHelpers.Value<S2TEdge>(e => Helpers.Satoshi2BTC(e.Value)),
        MappingHelpers.Height<S2TEdge>(e => e.BlockHeight),
        MappingHelpers.EdgeType<S2TEdge>(e => e.Type)
    ];

    public override string GetCsvHeader()
    {
        return _mappings.GetCsvHeader();
    }

    public override string GetCsv(IGraphComponent edge)
    {
        return GetCsv((S2TEdge)edge);
    }

    public static string GetCsv(S2TEdge edge)
    {
        return _mappings.GetCsv(edge);
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
    }
}