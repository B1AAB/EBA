using BC2G.Blockchains;
using BC2G.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.DAL
{
    internal class CoinbaseMapper : ScriptMapper
    {
        /// Note that the ordre of the items in this array should 
        /// match those in the `ToCSV` method.
        private readonly Prop[] _properties = new Prop[]
        {
            Prop.EdgeTargetAddress,
            Prop.EdgeTargetType,
            Prop.EdgeType,
            Prop.EdgeValue,
            Prop.Height
        };

        public CoinbaseMapper(
            string cypherImportPrefix,
            string importDirectory,
            string filename = "tmpBulkImportCoinbase.csv") :
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
                $"LOAD CSV WITH HEADERS FROM '{filename}' AS {Property.lineVarName} " +
                $"FIELDTERMINATOR '{csvDelimiter}' " +
                $"MATCH (coinbase:{BitcoinAgent.coinbase}) " +
                $"MERGE (target:{labels} {{" +
                $"{Props[Prop.EdgeTargetType].GetLoadExp(":")}, " +
                $"{Props[Prop.EdgeTargetAddress].GetLoadExp(":")}" +
                $"}}) " +
                $"WITH coinbase, target, {Property.lineVarName} " +
                $"MATCH (block:{BlockMapper.label} {{" +
                $"{Props[Prop.Height].GetLoadExp(":")}" +
                $"}}) " +
                $"CREATE (coinbase)-[:Generation {{" +
                $"{Props[Prop.EdgeType].GetLoadExp(":")}, " +
                $"{Props[Prop.EdgeValue].GetLoadExp(":")}, " +
                $"{Props[Prop.Height].GetLoadExp(":")}" +
                $"}}]->(target)" +
                "CREATE (block)-[:Creates]->(target)";
        }
    }
}
