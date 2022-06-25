using BC2G.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.DAL
{
    internal class EdgeBulkLoadMapper : ModelMapper<Edge>
    {
        public class Neo4jModel : Neo4jModelBase
        {
            public const string labels = "Script";
            public const string scriptType = "ScriptType";
            public const string scriptAddress = "Address";
            public const string edgeType = "EdgeType";
            public const string value = "Value";
        }

        public class CsvColumn
        {
            public const string sourceScriptAddress = "SourceScriptAddress";
            public const string sourceScriptType = "SourceScriptType";
            public const string targetScriptAddress = "TargetScriptAddress";
            public const string targetScriptType = "TargetScriptType";
            public const string edgeType = "EdgeType";
            public const string value = "Value";
            public const string height = "Height";
        }

        public EdgeBulkLoadMapper(
            string cypherImportPrefix,
            string importDirectory,
            string filename = "tmpBulkImportEdges.csv") :
            base(cypherImportPrefix, importDirectory, filename)
        { }

        public override string GetCsvHeader()
        {
            /// Note that the ordre of the items in this array should 
            /// match those in the `ToCSV` method.
            return string.Join(csvDelimiter, new string[]
            {
                CsvColumn.sourceScriptAddress,
                CsvColumn.sourceScriptType,
                CsvColumn.targetScriptAddress,
                CsvColumn.targetScriptType,
                CsvColumn.edgeType,
                CsvColumn.value,
                CsvColumn.height
            });
        }

        public override string ToCsv(Edge edge)
        {
            /// Note that the ordre of the items in this array should 
            /// match those in the `GetCsvHeader` method. 
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
                $"LOAD CSV WITH HEADERS FROM '{filename}' AS line " +
                $"FIELDTERMINATOR '{csvDelimiter}' " +
                $"MERGE (source:{Neo4jModel.labels} {{" +
                $"{Neo4jModel.scriptType}: line.{CsvColumn.sourceScriptType}, " +
                $"{Neo4jModel.scriptAddress}: line.{CsvColumn.sourceScriptAddress}" +
                "}) " +
                $"MERGE (target:{Neo4jModel.labels} {{" +
                $"{Neo4jModel.scriptType}: line.{CsvColumn.targetScriptType}, " +
                $"{Neo4jModel.scriptAddress}: line.{CsvColumn.targetScriptAddress}" +
                "}) " +
                "WITH source, target, line " +
                $"MATCH (block:{BlockBulkLoadMapper.Neo4jModel.label} {{" +
                $"{Neo4jModel.height}: line.{CsvColumn.height}" +
                "}) " +
                $"CREATE (source)-[:Redeems {{{Neo4jModel.height}: line.{CsvColumn.height}}}]->(block) " +
                $"CREATE (block)-[:Creates {{{Neo4jModel.height}: line.{CsvColumn.height}}}]->(target) " +
                "WITH source, target, line " +
                "CALL apoc.create.relationship(" +
                "source, " +
                $"line.{CsvColumn.edgeType}, " +
                $"{{" + 
                $"{Neo4jModel.value}: line.{CsvColumn.value}, " +
                $"{Neo4jModel.height}: line.{CsvColumn.height}" +
                $"}}, " +
                $"target)" +
                $"YIELD rel RETURN distinct 'done'";
        }
    }
}
