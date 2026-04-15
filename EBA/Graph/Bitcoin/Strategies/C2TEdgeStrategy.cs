namespace EBA.Graph.Bitcoin.Strategies;

public class C2TEdgeStrategy(bool serializeCompressed)
    : StrategyBase<C2TEdge, C2TEdgeStrategy>(
        $"edges_{C2TEdge.Kind.Source}_{C2TEdge.Kind.Relation}_{C2TEdge.Kind.Target}",
        serializeCompressed),
    IElementSchema<C2TEdge>
{
    public static string IdSpace { get; } = CoinbaseNode.Kind.ToString();

    public static EntityTypeMapper<C2TEdge> Mapper { get; } = new EntityTypeMapper<C2TEdge>(
        new MappingBuilder<C2TEdge>()
            .MapSourceId(IdSpace, _ => CoinbaseNode.Kind)
            .MapTargetId(TxNodeStrategy.IdSpace, e => e.Target.Txid)
            .MapValue(e => e.Value)
            .MapBlockHeight(e => e.BlockHeight)
            .MapEdgeType(e => e.Relation)
            .ToArray());

    public static C2TEdge Deserialize(TxNode target, IRelationship relationship)
    {
        return new C2TEdge(
            target: target,
            value: Mapper.GetValue(x => x.Value, relationship.Properties),
            timestamp: 0,
            blockHeight: Mapper.GetValue(x => x.BlockHeight, relationship.Properties));
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