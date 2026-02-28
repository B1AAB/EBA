using EBA.Utilities;

namespace EBA.Graph.Bitcoin.Strategies;

public class S2TEdgeStrategy(bool serializeCompressed)
    : BitcoinStrategyBase(
        $"edges_{S2TEdge.Kind.Source}_{S2TEdge.Kind.Relation}_{S2TEdge.Kind.Target}",
        serializeCompressed)
{
    private static readonly PropertyMapping<S2TEdge>[] _mappings =
    [
        PropertyMappingFactory.SourceId<S2TEdge>(ScriptNodeStrategy.IdSpace, e => e.Source.Id),
        PropertyMappingFactory.TargetId<S2TEdge>(TxNodeStrategy.IdSpace, e => e.Target.Txid),
        PropertyMappingFactory.ValueBTC<S2TEdge>(e => Helpers.Satoshi2BTC(e.Value)),
        PropertyMappingFactory.Height<S2TEdge>(e => e.BlockHeight),
        PropertyMappingFactory.SpentUtxos<S2TEdge>(nameof(S2TEdge.SpentUTxOs), e => e.SpentUTxOs),
        PropertyMappingFactory.EdgeType<S2TEdge>(e => e.Relation)
    ];

    public override string GetCsvHeader()
    {
        return _mappings.GetCsvHeader();
    }

    public override string GetCsvRow(IGraphElement edge)
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