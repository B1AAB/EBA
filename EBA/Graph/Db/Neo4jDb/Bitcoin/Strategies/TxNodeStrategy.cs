using EBA.Graph.Bitcoin;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class TxNodeStrategy(bool serializeCompressed) : StrategyBase(serializeCompressed)
{
    public const NodeLabels Label = NodeLabels.Tx;

    private static readonly PropertyMapping<TxNode>[] _mappings =
    [
        new(nameof(TxNode.Txid), FieldType.String, n => n.Txid, p => p.GetIdFieldCsvHeader(Label.ToString())),
        new(nameof(TxNode.Version), FieldType.Long, n => n.Version),
        new(nameof(TxNode.Size), FieldType.Long, n => n.Size),
        new(nameof(TxNode.VSize), FieldType.Long, n => n.VSize),
        new(nameof(TxNode.Weight), FieldType.Long, n => n.Weight),
        new(nameof(TxNode.LockTime), FieldType.Long, n => n.LockTime),

        new(":LABEL", FieldType.String, _ => Label, _ => ":LABEL"),
    ];

    public override string GetCsvHeader()
    {
        return _mappings.GetCsvHeader();
    }

    public override string GetCsv(IGraphComponent component)
    {
        return GetCsv((TxNode)component);
    }

    public static string GetCsv(TxNode node)
    {
        return _mappings.GetCsv(node);
    }

    public override string GetQuery(string filename)
    {
        // The following is an example of the generated query.
        //
        // LOAD CSV WITH HEADERS FROM 'file:///filename.csv'
        // AS line FIELDTERMINATOR '	'
        // MERGE (node:Tx {Txid:line.Txid})
        // SET
        //  node.Version = CASE line.SourceVersion WHEN "" THEN null ELSE toInteger(line.SourceVersion) END,
        //  node.Size = CASE line.SourceSize WHEN "" THEN null ELSE toInteger(line.SourceSize) END,
        //  node.VSize = CASE line.SourceVSize WHEN "" THEN null ELSE toInteger(line.SourceVSize) END,
        //  node.Weight = CASE line.SourceWeight WHEN "" THEN null ELSE toInteger(line.SourceWeight) END,
        //  node.LockTime = CASE line.SourceLockTime WHEN "" THEN null ELSE toInteger(line.SourceLockTime) END 
        //

        string l = Property.lineVarName, node = "node";

        var builder = new StringBuilder();
        /*
        builder.Append(
            $"LOAD CSV WITH HEADERS FROM '{filename}' AS {l} " +
            $"FIELDTERMINATOR '{Neo4jDbLegacy.csvDelimiter}' " +
            $"MERGE ({node}:{Label} {{{Props.Txid.GetSetter()}}}) ");

        builder.Append("SET ");
        builder.Append(string.Join(
            ", ",
            from x in _properties where x != Props.Txid select $"{x.GetSetterWithNullCheck(node)}"));
        */
        return builder.ToString();
    }
}