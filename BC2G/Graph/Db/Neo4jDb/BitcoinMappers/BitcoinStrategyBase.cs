namespace BC2G.Graph.Db.Neo4jDb.BitcoinMappers;

public abstract class BitcoinEdgeStrategy : StrategyBase
{
    public static string GetCreatesEdgeQuery(string blockVar = "block", string targetVar = "target")
    {
        return $"MERGE ({blockVar})-[:Creates {{{Props.Height.GetSetter()}}}]->({targetVar})";
    }

    public static string GetRedeemsEdgeQuery(string blockVar = "block", string sourceVar = "source")
    {
        return $"MERGE ({sourceVar})-[:Redeems {{{Props.Height.GetSetter()}}}]->({blockVar})";
    }

    public static string GetEdgeQuery(
        List<Property> props,
        string sourceVar = "source",
        string targetVar = "target")
    {
        // An example of the populated template
        // (indentation added for better readibility).
        //
        // CALL apoc.merge.relationship(
        //     source,
        //     line.EdgeType,
        //     {
        //         Value:toFloat(line.Value),
        //         Height:toInteger(line.Height)
        //     },
        //     { Count : 0},
        //     target,
        //     {}
        // )
        // YIELD rel SET rel.Count = rel.Count + 1
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
