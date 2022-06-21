using BC2G.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.DAL
{
    internal class CoinbaseTxBulkLoadMapper : ModelMapper<Edge>
    {
        private const string _scriptType = nameof(Edge.Target.ScriptType);
        private const string _address = nameof(Edge.Target.Address);
        private static readonly string _edgeType = EdgeBlockBulkMapper.EdgeType;
        private static readonly string _valueName = EdgeBlockBulkMapper.ValueName;
        private const string _blockHeight = nameof(Edge.BlockHeight);

        /// <summary>
        /// Note that the ordre of the items in this array should 
        /// match those in the `GetCsvHeader` method.
        /// </summary>
        private static readonly string[] _properties = new string[]
        {
            _scriptType,
            _address,
            _edgeType,
            _valueName,
            _blockHeight
        };

        public override string GetLabels()
        {
            return GraphDB.Coinbase;
        }

        public override string GetCsvHeader()
        {
            return string.Join(csvDelimiter, _properties);
        }

        public override string ToCsv(Edge edge)
        {
            /// Note that the order in this array 
            /// should match those in `_properties`. 
            return string.Join(csvDelimiter, new string[]
            {
                edge.Target.ScriptType.ToString(),
                edge.Target.Address,
                edge.Type.ToString(),
                edge.Value.ToString(),
                edge.BlockHeight.ToString()
            });
        }

        public override string GetCypherQuery(string filename)
        {
            var builder = new StringBuilder();
            builder.Append(
                $"LOAD CSV WITH HEADERS FROM '{filename}' AS line " +
                $"FIELDTERMINATOR '{csvDelimiter}' ");

            builder.Append($"MATCH (coinbase:{GetLabels()}) ");

            builder.Append($"MERGE " +
                $"(target:{EdgeBlockBulkMapper.Labels} {{" +
                $"{nameof(Edge.Target.ScriptType)}: line.{_scriptType}, " +
                $"{nameof(Edge.Target.Address)}: line.{_address}" +
                $"}}) ");

            builder.Append("WITH coinbase, target, line ");

            builder.Append(
                $"MATCH (block:{BlockBulkLoadMapper.Labels} {{" +
                $"{BlockBulkLoadMapper.HeightPropertyName}: line.{_blockHeight}" +
                $"}}) ");

            // TODO: change generation in the following to read from the CSV file.
            builder.Append(
                $"CREATE (coinbase)-[:Generation {{" +
                $"{_edgeType}: line.{_edgeType}, " +
                $"{_valueName}: line.{_valueName}, " +
                $"{_blockHeight}: line.{_blockHeight}" +
                $"}}]->(target)");

            builder.Append("CREATE (block)-[:Creates]->(target)");

            return builder.ToString();
        }
    }
}
