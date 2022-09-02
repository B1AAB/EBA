using BC2G.Graph;
using BC2G.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.DAL.Bulkload
{
    internal class ScriptMapper : ModelMapper<Edge>
    {
        public const string labels = "Script";

        /// Note that the ordre of the items in this array should 
        /// match those in the `ToCSV` method.
        private readonly Prop[] _properties = new Prop[]
        {
            Prop.EdgeSourceAddress,
            Prop.EdgeSourceType,
            Prop.EdgeTargetAddress,
            Prop.EdgeTargetType,
            Prop.EdgeType,
            Prop.EdgeValue,
            Prop.Height
        };

        public ScriptMapper(
            string workingDirectory,
            string cypherImportPrefix,
            //string importDirectory,
            string filename = "tmpBulkImportEdges.csv") :
            base(workingDirectory, cypherImportPrefix, /*importDirectory,*/ filename)
        { }

        public override string GetCsvHeader()
        {
            return string.Join(csvDelimiter,
                from x in _properties select Props[x].CsvHeader);
        }

        public override string ToCsv(Edge edge)
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

        protected override string ComposeCypherQuery(string filename)
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

            var l = Property.lineVarName;
            var unknown = nameof(ScriptType.Unknown);

            return
                $"LOAD CSV WITH HEADERS FROM '{filename}' AS {l} " +
                $"FIELDTERMINATOR '{csvDelimiter}' " +
                // Load source
                $"MERGE (source:{labels} {{" +
                $"{Props[Prop.EdgeSourceAddress].GetLoadExp(":")}}}) " +
                $"ON CREATE SET source.{Props[Prop.EdgeSourceType].GetLoadExp("=")} " +
                $"ON MATCH SET source.{Props[Prop.EdgeSourceType].Name} = " +
                $"CASE {l}.{Props[Prop.EdgeSourceType].CsvHeader} " +
                $"WHEN '{unknown}' THEN source.{Props[Prop.EdgeSourceType].Name} " +
                $"ELSE {l}.{Props[Prop.EdgeSourceType].CsvHeader} " +
                $"END " +
                // Load target
                $"MERGE (target:{labels} {{" +
                $"{Props[Prop.EdgeTargetAddress].GetLoadExp(":")}}}) " +
                $"ON CREATE SET target.{Props[Prop.EdgeTargetType].GetLoadExp("=")} " +
                $"ON MATCH SET target.{Props[Prop.EdgeTargetType].Name} = " +
                $"CASE {l}.{Props[Prop.EdgeTargetType].CsvHeader} " +
                $"WHEN '{unknown}' THEN target.{Props[Prop.EdgeTargetType].Name} " +
                $"ELSE {l}.{Props[Prop.EdgeTargetType].CsvHeader} " +
                $"END " +
                $"WITH source, target, {l} " +
                // Find the block
                $"MATCH (block:{BlockMapper.label} {{" +
                $"{Props[Prop.Height].GetLoadExp(":")}" +
                "}) " +
                // Create relationship between the block node and the scripts nodes. 
                RedeemsEdgeQuery +
                CreatesEdgeQuery +
                $"WITH source, target, {l} " +
                // Create relationship between the source and target scripts,
                // where the type of the relationship is read from the CSV file.
                "CALL apoc.merge.relationship(" +
                "source, " + // source
                $"{l}.{Props[Prop.EdgeType].CsvHeader}, " + // relationship type
                $"{{" + // properties
                $"{Props[Prop.EdgeValue].GetLoadExp(":")}, " + 
                $"{Props[Prop.Height].GetLoadExp(":")}" +
                $"}}, " +
                $"{{ Count : 0}}, " + // on create
                $"target, " + // target
                $"{{}}" + // on update
                $")" + 
                $"YIELD rel " +
                $"SET rel.Count = rel.Count + 1 " +
                $"RETURN distinct 'DONE'";
        }
    }
}
