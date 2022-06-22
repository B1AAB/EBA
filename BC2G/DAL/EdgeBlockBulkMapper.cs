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
        public const string neo4jModelLabels = "Script";
        public const string neo4jModelScriptType = "ScriptType";
        public const string neo4jModelScriptAddress = "Address";
        public const string neo4jModelEdgeType = "EdgeType";
        public const string neo4jModelValue = "Value";
        public const string neo4jModelBlockHeight = "Height";

        public const string csvHeaderSourceScriptAddress = "SourceScriptAddress";
        public const string csvHeaderSourceScriptType = "SourceScriptType";
        public const string csvHeaderTargetScriptAddress = "TargetScriptAddress";
        public const string csvHeaderTargetScriptType = "TargetScriptType";
        public const string csvHeaderEdgeType = "EdgeType";
        public const string csvHeaderValue = "Value";
        public const string csvHeaderBlockHeight = "BlockHeight";

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
                csvHeaderSourceScriptAddress,
                csvHeaderSourceScriptType,
                csvHeaderTargetScriptAddress,
                csvHeaderTargetScriptType ,
                csvHeaderEdgeType,
                csvHeaderValue,
                csvHeaderBlockHeight
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
            return
                $"LOAD CSV WITH HEADERS FROM '{filename}' AS line " +
                $"FIELDTERMINATOR '{csvDelimiter}' " +
                $"MERGE (source:{neo4jModelLabels} {{" +
                $"{neo4jModelScriptType}: line.{csvHeaderSourceScriptType}, " +
                $"{neo4jModelScriptAddress}: line.{csvHeaderSourceScriptAddress}" +
                "}) " +
                $"MERGE (target:{neo4jModelLabels} {{" +
                $"{neo4jModelScriptType}: line.{csvHeaderTargetScriptType}, " +
                $"{neo4jModelScriptAddress}: line.{csvHeaderTargetScriptAddress}" +
                "}) " +
                "WITH source, target, line " +
                $"MATCH (block:{BlockBulkLoadMapper.neo4jModelLabel} {{" +
                $"{BlockBulkLoadMapper.neo4jModelHeight}: line.{csvHeaderBlockHeight}" +
                "}) " +
                "CREATE (source)-[:Redeems]->(block) " +
                "CREATE (block)-[:Creates]->(target) " +
                "WITH source, target, line " +
                "CALL apoc.create.relationship(" +
                "source, " +
                $"line.{csvHeaderEdgeType}, {{" +
                $"{neo4jModelValue}: line.{csvHeaderValue}, " +
                $"{neo4jModelBlockHeight}: line.{csvHeaderBlockHeight}" +
                $"}}, " +
                $"target) YIELD rel RETURN distinct 'done'";
        }
    }
}
