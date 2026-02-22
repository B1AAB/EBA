using EBA.Utilities;

namespace EBA.Graph.Bitcoin.Strategies;

public class T2SEdgeStrategy(bool serializeCompressed) 
    : BitcoinStrategyBase(
        $"{T2SEdge.Kind.Source}_{T2SEdge.Kind.Relation}_{T2SEdge.Kind.Target}_edges.csv",
        serializeCompressed)
{
    public static readonly PropertyMapping<T2SEdge>[] _mappings =
    [
        PropertyMappingFactory.SourceId<T2SEdge>(TxNodeStrategy.IdSpace, e => e.Source.Txid),
        PropertyMappingFactory.TargetId<T2SEdge>(ScriptNodeStrategy.IdSpace, e => e.Target.Id),
        PropertyMappingFactory.ValueBTC<T2SEdge>(e => Helpers.Satoshi2BTC(e.Value)),
        PropertyMappingFactory.Height<T2SEdge>(e => e.BlockHeight),
        PropertyMappingFactory.EdgeType<T2SEdge>(e => e.Relation)
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