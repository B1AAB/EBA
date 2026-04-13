using Factory = EBA.Graph.Bitcoin.Strategies.PropertyMappingFactory;

namespace EBA.Graph.Bitcoin.Strategies;

public class C2TEdgeStrategy(bool serializeCompressed) 
    : BitcoinStrategyBase(
        $"edges_{C2TEdge.Kind.Source}_{C2TEdge.Kind.Relation}_{C2TEdge.Kind.Target}",
        serializeCompressed)
{
    public static string IdSpaceCoinbase { get; } = CoinbaseNode.Kind.ToString();

    public static readonly PropertyMapping<C2TEdge>[] Mappings = new MappingBuilder<C2TEdge>()
        .MapSourceId(IdSpaceCoinbase, _ => CoinbaseNode.Kind)
        .MapTargetId(TxNodeStrategy.IdSpace, e => e.Target.Txid)
        .MapValue(e => e.Value)
        .MapBlockHeight(e => e.BlockHeight)
        .MapEdgeType(e => e.Relation)
        .ToArray();

    public override string GetCsvHeader()
    {
        return Mappings.GetCsvHeader();
    }

    public override string GetCsvRow(IGraphElement edge)
    {
        return GetCsv((C2TEdge)edge);
    }

    public static string GetCsv(C2TEdge edge)
    {
        return Mappings.GetCsv(edge);
    }

    public static C2TEdge Deserialize(TxNode target, IRelationship relationship)
    {
        return new C2TEdge(
            target: target,
            value: Mappings.Get(Factory.ValueProperty.Name).Deserialize<long>(relationship.Properties),
            timestamp: 0,
            blockHeight: Mappings.Get(Factory.HeightProperty.Name).Deserialize<long>(relationship.Properties));
    }

    public override string GetQuery(string csvFilename)
    {
        // The following is an example of the query this method generates.
        // Indentation and linebreaks are added for the readability and 
        // not included in the gerated queries.
        //
        //
        // LOAD CSV WITH HEADERS FROM 'file:///filename.csv' AS line
        // FIELDTERMINATOR '	'
        //
        // MATCH (coinbase:Coinbase)
        // MATCH (target:Tx {Txid:line.TargetId})
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

        string l = Property.lineVarName, s = "coinbase", t = "target", b = "block";
        /*
        var builder = new StringBuilder(
            $"LOAD CSV WITH HEADERS FROM '{csvFilename}' AS {l} " +
            $"FIELDTERMINATOR '{Neo4jDbLegacy.csvDelimiter}' ");
        
        builder.Append(
            $"MATCH ({s}:{NodeLabels.Coinbase}) " +
            $"MATCH ({t}:{TxNodeStrategy.Label} {{{Props.T2TEdgeTargetTxid.GetSetter()}}}) " +
            $"MATCH ({b}:{BlockNodeStrategy.Label} {{{Props.Height.GetSetter()}}}) ");

        builder.Append(GetCreatesEdgeQuery(b, t) + " ");
        builder.Append($"WITH {l}, {b}, {s}, {t} ");

        builder.Append(GetApocCreateEdgeQuery(GetEdgePropertiesBase(), s, t));
        builder.Append(" RETURN distinct 'DONE'");
        
        return builder.ToString();*/
        return "";
    }
}