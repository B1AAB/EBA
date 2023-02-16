using INode = BC2G.Graph.Model.INode;

namespace BC2G.Graph.Db.Neo4j.BitcoinMappers;

public class S2SEdgeMapper : BitcoinEdgeMapper
{
    public const string labels = "Script";

    /// Note that the ordre of the items in this array should 
    /// match those in the `GetCsv` method.
    private readonly Property[] _properties = new Property[]
    {
        Props.EdgeSourceAddress,
        Props.EdgeSourceType,
        Props.EdgeTargetAddress,
        Props.EdgeTargetType,
        Props.EdgeType,
        Props.EdgeValue,
        Props.Height
    };

    public override string GetCsvHeader()
    {
        return string.Join(
            csvDelimiter,
            from x in _properties select x.CsvHeader);
    }

    public override string GetCsv(IEdge<INode, INode> edge)
    {
        return GetCsv((S2SEdge)edge);
    }

    public static string GetCsv(S2SEdge edge)
    {
        /// Note that the ordre of the items in this array should 
        /// match those in the `_properties`. 

        return string.Join(csvDelimiter, new string[]
        {
            edge.Source.Address,
            edge.Source.ScriptType.ToString(),
            edge.Target.Address,
            edge.Target.ScriptType.ToString(),
            edge.Type.ToString(),
            edge.Value.ToString(),
            edge.BlockHeight.ToString()
        });
    }

    public override string GetQuery(string csvFilename)
    {
        /// Loading `script` type: 
        /// Script address should be unique. If simply 
        /// merging on Script Type and Address, it may end-up
        /// trying to create two nodes with the same address
        /// (hence violating the address uniqueness requirment),
        /// because it is possible to have two scripts with the 
        /// same address one of type 'Unknown' and other of 
        /// another type. Hence, we use the two logics for 
        /// "ON CREATE" and "ON MATCH". The former creates 
        /// the node as read from the CSV. The latter, merges
        /// scripts by replacing 'Unknown' script type, with the 
        /// type of the other script if it is not 'Unknown'.

        string l = Property.lineVarName, s = "source", t = "target", b="block";


        return
            $"LOAD CSV WITH HEADERS FROM '{csvFilename}' AS {l} " +
            $"FIELDTERMINATOR '{csvDelimiter}' " +
            // Load source
            GetNodeQuery(s, labels, Props.EdgeSourceAddress, Props.EdgeSourceType) +
            " " +

            // Load target
            GetNodeQuery(t, labels, Props.EdgeTargetAddress, Props.EdgeTargetType) +
            " " +

            $"WITH {s}, {t}, {l} " +
            // Find the block
            GetBlockQuery(b) + 
            " " +

            // Create relationship between the block node and the scripts nodes. 
            RedeemsEdgeQuery +
            CreatesEdgeQuery +
            $"WITH {s}, {t}, {l} " +
            // Create relationship between the source and target scripts,
            // where the type of the relationship is read from the CSV file.

            GetEdgeQuery(new List<Property>() { Props.EdgeValue, Props.Height }, s, t) +
            " " +
            $"RETURN distinct 'DONE'";
    }
}
