using BC2G.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.DAL
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
            string cypherImportPrefix,
            string importDirectory,
            string filename = "tmpBulkImportEdges.csv") :
            base(cypherImportPrefix, importDirectory, filename)
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
            /// There are some corner cases where there exist
            /// more than one transactions sending value between 
            /// same input and output (and possibly same value). 
            /// One of the design decisions of BC2G is to sum 
            /// these transactions and represent them with only one.
            /// However, in order to leave this design decision 
            /// make in one place, we use `apoc.create.relationship` 
            /// in the following where if two transfers between 
            /// same inputs and outputs in a given block are given 
            /// in the CSV file, that leads to the creation of two 
            /// edges. Alternative is using `apoc.merge.relationship`
            /// where it can ensure the source-target-properties 
            /// tuple is unique. 

            return
                $"LOAD CSV WITH HEADERS FROM '{filename}' AS {Property.lineVarName} " +
                $"FIELDTERMINATOR '{csvDelimiter}' " +
                $"MERGE (source:{labels} {{" +
                $"{Props[Prop.EdgeSourceType].GetLoadExp(":")}, " +
                $"{Props[Prop.EdgeSourceAddress].GetLoadExp(":")}" +
                "}) " +
                $"MERGE (target:{labels} {{" +
                $"{Props[Prop.EdgeTargetType].GetLoadExp(":")}, " +
                $"{Props[Prop.EdgeTargetAddress].GetLoadExp(":")}" +
                "}) " +
                "WITH source, target, line " +
                $"MATCH (block:{BlockMapper.label} {{" +
                $"{Props[Prop.Height].GetLoadExp(":")}" +
                "}) " +
                $"CREATE (source)-[:Redeems {{{Props[Prop.Height].GetLoadExp(":")}}}]->(block) " +
                $"CREATE (block)-[:Creates {{{Props[Prop.Height].GetLoadExp(":")}}}]->(target) " +
                "WITH source, target, line " +
                "CALL apoc.create.relationship(" +
                "source, " +
                $"{Property.lineVarName}.{Props[Prop.EdgeType].CsvHeader}, " +
                $"{{" +
                $"{Props[Prop.EdgeValue].GetLoadExp(":")}, " +
                $"{Props[Prop.Height].GetLoadExp(":")}" +
                $"}}, " +
                $"target)" +
                $"YIELD rel RETURN distinct 'done'";
        }
    }
}
