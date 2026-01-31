using EBA.Graph.Bitcoin;
using EBA.Utilities;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class C2SEdgeStrategy(bool serializeCompressed) : S2SEdgeStrategy(serializeCompressed)
{
    private static readonly PropertyMapping<C2SEdge>[] _mappings =
    [
        PropertyMappingFactory.SourceId<C2SEdge>(NodeLabels.Coinbase, _ => NodeLabels.Coinbase),
        PropertyMappingFactory.TargetId<C2SEdge>(ScriptNodeStrategy.Label, e => e.Target.Address),
        PropertyMappingFactory.ValueBTC<C2SEdge>(e => Helpers.Satoshi2BTC(e.Value)),
        PropertyMappingFactory.Height<C2SEdge>(e => e.BlockHeight),
        PropertyMappingFactory.EdgeType<C2SEdge>(e => e.Type)
    ];

    public override string GetCsvHeader()
    {
        return _mappings.GetCsvHeader();
    }

    public override string GetCsv(IGraphComponent edge)
    {
        return GetCsv((C2SEdge)edge);
    }

    public static string GetCsv(C2SEdge edge)
    {
        return _mappings.GetCsv(edge);
    }

    public override string GetQuery(string csvFilename)
    {
        // The following is an example of the query this method generates.
        // Indentation and line breaks are added for the readiblity and 
        // are not included in the generated query.
        //
        //
        // LOAD CSV WITH HEADERS FROM 'file:///filename.csv'
        // AS line FIELDTERMINATOR '	'
        //
        // MATCH (coinbase:Coinbase)
        // MATCH (target:Script {Address:line.TargetAddress})
        // MATCH (block:Block {Height:toInteger(line.Height)})
        //
        // CREATE (block)-[:Creates {Height:toInteger(line.Height), Value:toFloat(line.Value)}]->(target)
        //
        // WITH line, block, coinbase, target
        //
        // CALL apoc.create.relationship(
        //     coinbase,
        //     line.EdgeType,
        //     {
        //         Height:toInteger(line.Height),
        //         Value:toFloat(line.Value)
        //     },
        //     target)
        // YIELD rel
        // RETURN distinct 'DONE'
        //
        /*
        string l = Property.lineVarName, b = "block", s = "coinbase", t = "target";

        var builder = new StringBuilder(
            $"LOAD CSV WITH HEADERS FROM '{csvFilename}' AS {l} " +
            $"FIELDTERMINATOR '{Neo4jDbLegacy.csvDelimiter}' ");

        builder.Append(
            $"MATCH ({s}:{NodeLabels.Coinbase}) " +
            $"MATCH ({t}:{ScriptNodeStrategy.Label} {{{Props.EdgeTargetAddress.GetSetter()}}}) " +
            $"MATCH ({b}:{BlockNodeStrategy.Label} {{{Props.Height.GetSetter()}}}) ");

        builder.Append(GetCreatesEdgeQuery(b, t) + " ");
        builder.Append($"WITH {l}, {b}, {s}, {t} ");

        builder.Append(GetApocCreateEdgeQuery(GetEdgePropertiesBase(), s, t));
        builder.Append(" RETURN distinct 'DONE'");

        return builder.ToString();*/
        throw new NotImplementedException();
    }
}