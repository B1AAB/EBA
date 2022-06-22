using BC2G.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.DAL
{
    internal class CoinbaseTxBulkLoadMapper : EdgeBulkLoadMapper
    {
        public CoinbaseTxBulkLoadMapper(
            string cypherImportPrefix,
            string importDirectory,
            string filename = "tmpBulkImportCoinbase.csv") :
            base(cypherImportPrefix, importDirectory, filename)
        { }

        public override string GetCsvHeader()
        {
            /// Note that the ordre of the items in this array should 
            /// match those in the `ToCSV` method.
            return string.Join(csvDelimiter, new string[]
            {
                csvHeaderTargetScriptType,
                csvHeaderTargetScriptAddress,
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
                edge.Target.ScriptType.ToString(),
                edge.Target.Address,
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
                $"MATCH (coinbase:{neo4jModelLabels}) " +
                $"MERGE (target:{neo4jModelLabels} {{" +
                $"{neo4jModelScriptType}: line.{csvHeaderTargetScriptAddress}, " +
                $"{neo4jModelScriptAddress}: line.{csvHeaderTargetScriptAddress}" +
                $"}}) " +
                "WITH coinbase, target, line " +
                $"MATCH (block:{BlockBulkLoadMapper.neo4jModelLabel} {{" +
                $"{BlockBulkLoadMapper.neo4jModelHeight}: line.{csvHeaderBlockHeight}" +
                $"}}) " +
                $"CREATE (coinbase)-[:Generation {{" +
                $"{neo4jModelEdgeType}: line.{csvHeaderEdgeType}, " +
                $"{neo4jModelValue}: line.{csvHeaderValue}, " +
                $"{neo4jModelBlockHeight}: line.{csvHeaderBlockHeight}" +
                $"}}]->(target)" +
                "CREATE (block)-[:Creates]->(target)";
        }
    }
}
