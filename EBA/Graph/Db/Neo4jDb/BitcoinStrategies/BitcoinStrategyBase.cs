namespace BC2G.Graph.Db.Neo4jDb.BitcoinStrategies;

public abstract class BitcoinEdgeStrategy(bool serializeCompressed) : StrategyBase(serializeCompressed)
{
    public static string GetCreatesEdgeQuery(string blockVar = "block", string targetVar = "target")
    {
        // The following is an example of the query this method generates.
        //
        // CREATE (block)-[:Creates {Height:toInteger(line.Height), Value:toFloat(line.Value)}]->(target)
        //

        var builder = new StringBuilder(
            $"CREATE ({blockVar})-[:{Property.createsEdgeLabel} {{");

        builder.Append(
            string.Join(", ", from x in GetEdgePropertiesBase() select x.GetSetter()));

        builder.Append(
            $"}}]->({targetVar})");

        return builder.ToString();
    }

    public static string GetRedeemsEdgeQuery(string blockVar = "block", string sourceVar = "source")
    {
        // The following is an example of the query this method generates.
        //
        // CREATE (source)-[:Redeems {Height:toInteger(line.Height), Value:toFloat(line.Value)}]->(block)
        //

        var builder = new StringBuilder(
            $"CREATE ({sourceVar})-[:{Property.redeemsEdgeLabel} {{");

        builder.Append(
            string.Join(", ", from x in GetEdgePropertiesBase() select x.GetSetter()));

        builder.Append(
            $"}}]->({blockVar})");

        return builder.ToString();
    }

    public static ReadOnlyCollection<Property> GetEdgePropertiesBase()
    {
        return new ReadOnlyCollection<Property>([Props.Height, Props.EdgeValue]);
    }

    public static string GetApocCreateEdgeQuery(
        IEnumerable<Property> props,
        string sourceVar = "source",
        string targetVar = "target")
    {
        // The following is an example of the query this method generates.
        // Indentation is added for better readability and does not exist 
        // in the generated query.
        //
        // CALL apoc.create.relationship(
        //     source,
        //     line.EdgeType,
        //     {
        //         Value:toFloat(line.Value),
        //         Height:toInteger(line.Height)
        //     },
        //     target)
        // YIELD rel
        //

        var builder = new StringBuilder(
            $"CALL apoc.create.relationship(" +
            $"{sourceVar}, " +
            $"{Property.lineVarName}.{Props.EdgeType.CsvHeader}, " +
            $"{{");

        builder.Append(string.Join(", ", from x in props select x.GetSetter()));

        builder.Append(
            $"}}, " +
            $"{targetVar}) " +
            $"YIELD rel");

        return builder.ToString();
    }

    public static string GetApocMergeEdgeQuery(
        List<Property> props,
        string sourceVar = "source",
        string targetVar = "target")
    {
        // The following is an example of the query this method generates.
        // Indentation added for better readibility and does not exist 
        // in the generated query.
        //
        // CALL apoc.merge.relationship(
        //     coinbase,
        //     line.EdgeType,
        //     {
        //         Value:toFloat(line.Value),
        //         Height:toInteger(line.Height)
        //     },
        //     {
        //         Count : 0
        //     },
        //     target,
        //     {})
        // YIELD rel
        // SET rel.Count = rel.Count + 1
        //

        var builder = new StringBuilder(
            "CALL apoc.merge.relationship(" +
            $"{sourceVar}, " +
            $"{Property.lineVarName}.{Props.EdgeType.CsvHeader}, " +
            $"{{");

        builder.Append(string.Join(", ", from x in props select x.GetSetter()));

        builder.Append(
            $"}}, " +
            $"{{ Count : 0}}, " + // on create
            $"{targetVar}, " +
            $"{{}}" +             // on update
            $") " +
            $"YIELD rel " +
            $"SET rel.Count = rel.Count + 1");

        return builder.ToString();
    }
}
