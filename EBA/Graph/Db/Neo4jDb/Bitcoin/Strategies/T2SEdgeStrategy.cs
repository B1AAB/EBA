using EBA.Utilities;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class T2SEdgeStrategy(bool serializeCompressed) : BitcoinEdgeStrategy(serializeCompressed)
{
    public static readonly PropertyMapping<T2SEdge>[] _mappings =
    [
        MappingHelpers.SourceId<T2SEdge>(TxNodeStrategy.Label, e => e.Source.Txid),
        MappingHelpers.TargetId<T2SEdge>(ScriptNodeStrategy.Label, e => e.Target.Address),
        MappingHelpers.Value<T2SEdge>(e => Helpers.Satoshi2BTC(e.Value)),
        MappingHelpers.Height<T2SEdge>(e => e.BlockHeight),
        MappingHelpers.EdgeType<T2SEdge>(e => e.Type)
    ];

    public override string GetCsvHeader()
    {
        return _mappings.GetCsvHeader();
    }

    public override string GetCsv(IGraphComponent edge)
    {
        return GetCsv((T2SEdge)edge);
    }

    public static string GetCsv(T2SEdge edge)
    {
        return _mappings.GetCsv(edge);
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
    }
}