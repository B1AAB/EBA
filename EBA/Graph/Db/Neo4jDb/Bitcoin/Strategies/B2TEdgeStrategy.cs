using EBA.Utilities;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class B2TEdgeStrategy(bool serializeCompressed) : BitcoinEdgeStrategy(serializeCompressed)
{
    private static readonly PropertyMapping<B2TEdge>[] _mappings =
    [
        MappingHelpers.SourceId<B2TEdge>(BlockNodeStrategy.Label, e => e.Source.BlockMetadata.Height),
        MappingHelpers.TargetId<B2TEdge>(TxNodeStrategy.Label, e => e.Target.Txid),
        MappingHelpers.ValueBTCMapper<B2TEdge>(e => Helpers.Satoshi2BTC(e.Value)),
        MappingHelpers.HeightMapper<B2TEdge>(e => e.BlockHeight),
        MappingHelpers.EdgeType<B2TEdge>(e => e.Type)
    ];

    public override string GetCsvHeader()
    {
        return _mappings.GetCsvHeader();
    }

    public override string GetCsv(IGraphComponent edge)
    {
        return GetCsv((B2TEdge)edge);
    }

    public static string GetCsv(B2TEdge edge)
    {
        return _mappings.GetCsv(edge);
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
    }
}