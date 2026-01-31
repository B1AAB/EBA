using EBA.Graph.Bitcoin;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class TxNodeStrategy(bool serializeCompressed) : StrategyBase(serializeCompressed)
{
    public const NodeLabels Label = NodeLabels.Tx;

    private const TxNode v = null!;
    private static readonly PropertyMapping<TxNode>[] _mappings =
    [
        MappingHelpers.TxIdMapper<TxNode>(n => n.Txid, p => p.GetIdFieldCsvHeader(Label.ToString())),
        new(nameof(v.Version), FieldType.Long, n => n.Version),
        new(nameof(v.Size), FieldType.Long, n => n.Size),
        new(nameof(v.VSize), FieldType.Long, n => n.VSize),
        new(nameof(v.Weight), FieldType.Long, n => n.Weight),
        new(nameof(v.LockTime), FieldType.Long, n => n.LockTime),

        new(":LABEL", FieldType.String, _ => Label, _ => ":LABEL"),
    ];

    private static readonly Dictionary<string, PropertyMapping<TxNode>> _mappingsDict =
        _mappings.ToDictionary(m => m.Property.Name, m => m);

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

    public static TxNode GetNodeFromProps(
        Neo4j.Driver.INode node,
        double originalIndegree,
        double originalOutdegree,
        double hopsFromRoot)
    {
        return new TxNode(
            txid: _mappingsDict[nameof(v.Txid)].Deserialize<string>(node.Properties) ?? 
                throw new ArgumentNullException(nameof(v.Txid)),
            version: _mappingsDict[nameof(v.Version)].Deserialize<ulong>(node.Properties),
            size: _mappingsDict[nameof(v.Size)].Deserialize<int>(node.Properties),
            vSize: _mappingsDict[nameof(v.VSize)].Deserialize<int>(node.Properties),
            weight: _mappingsDict[nameof(v.Weight)].Deserialize<int>(node.Properties),
            lockTime: _mappingsDict[nameof(v.LockTime)].Deserialize<long>(node.Properties),
            originalIndegree: originalIndegree,
            originalOutdegree: originalOutdegree,
            hopsFromRoot: hopsFromRoot,
            idInGraphDb: node.ElementId);
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

        /*string l = Property.lineVarName, node = "node";

        var builder = new StringBuilder();
        
        builder.Append(
            $"LOAD CSV WITH HEADERS FROM '{filename}' AS {l} " +
            $"FIELDTERMINATOR '{Neo4jDbLegacy.csvDelimiter}' " +
            $"MERGE ({node}:{Label} {{{Props.Txid.GetSetter()}}}) ");

        builder.Append("SET ");
        builder.Append(string.Join(
            ", ",
            from x in _properties where x != Props.Txid select $"{x.GetSetterWithNullCheck(node)}"));
        
        return builder.ToString();*/
        throw new NotImplementedException();
    }
}