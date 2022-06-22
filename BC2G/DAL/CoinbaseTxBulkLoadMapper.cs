using BC2G.Blockchains;
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
                CsvColumn.targetScriptType,
                CsvColumn.targetScriptAddress,
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
                $"MATCH (coinbase:{BitcoinAgent.coinbase}) " +
                $"MERGE (target:{Neo4jModel.labels} {{" +
                $"{Neo4jModel.scriptType}: line.{CsvColumn.targetScriptType}, " +
                $"{Neo4jModel.scriptAddress}: line.{CsvColumn.targetScriptAddress}" +
                $"}}) " +
                "WITH coinbase, target, line " +
                $"MATCH (block:{BlockBulkLoadMapper.Neo4jModel.label} {{" +
                $"{Neo4jModel.height}: line.{CsvColumn.height}" +
                $"}}) " +
                $"CREATE (coinbase)-[:Generation {{" +
                $"{Neo4jModel.edgeType}: line.{CsvColumn.edgeType}, " +
                $"{Neo4jModel.value}: line.{CsvColumn.value}, " +
                $"{Neo4jModel.height}: line.{CsvColumn.height}" +
                $"}}]->(target)" +
                "CREATE (block)-[:Creates]->(target)";
        }
    }
}
