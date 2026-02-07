using EBA.Graph.Bitcoin;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class NullDataNodeStrategy(bool serializeCompressed) : StrategyBase(serializeCompressed)
{
    public const NodeLabels Label = NodeLabels.NullData;

    private const NullDataNode v = null!;
    private static readonly PropertyMapping<NullDataNode>[] _mappings =
    [
        new(nameof(v.Id), FieldType.String, n => n.Id, p => p.GetIdFieldCsvHeader(Label.ToString())),
        new(nameof(v.HexBase64), FieldType.String, n => n.HexBase64),
        new(":LABEL", FieldType.String, _ => Label, _ => ":LABEL"),
    ];

    public override string GetCsvHeader()
    {
        return _mappings.GetCsvHeader();
    }

    public override string GetCsvRow(IGraphComponent component)
    {
        return GetCsv((NullDataNode)component);
    }

    public static string GetCsv(NullDataNode node)
    {
        return _mappings.GetCsv(node);
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
    }
}
