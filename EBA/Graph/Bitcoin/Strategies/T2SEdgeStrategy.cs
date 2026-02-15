using EBA.Utilities;

namespace EBA.Graph.Bitcoin.Strategies;

public class T2SEdgeStrategy(bool serializeCompressed) : BitcoinEdgeStrategy(serializeCompressed)
{
    public static readonly PropertyMapping<T2SEdge>[] _mappings =
    [
        PropertyMappingFactory.SourceId<T2SEdge>(TxNodeStrategy.Label, e => e.Source.Txid),
        PropertyMappingFactory.TargetId<T2SEdge>(ScriptNodeStrategy.Label, e => e.Target.Address),
        PropertyMappingFactory.ValueBTC<T2SEdge>(e => Helpers.Satoshi2BTC(e.Value)),
        PropertyMappingFactory.Height<T2SEdge>(e => e.BlockHeight),
        PropertyMappingFactory.EdgeType<T2SEdge>(e => e.Type)
    ];

    public override string GetCsvHeader()
    {
        return _mappings.GetCsvHeader();
    }

    public override string GetCsvRow(IGraphElement edge)
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