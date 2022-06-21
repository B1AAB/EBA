using BC2G.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.DAL
{
    internal class EdgeBlockBulkMapper : ModelMapper<Edge>
    {
        public static string Labels { get { return "Script"; } }
        public static string EdgeType { get { return nameof(Edge.Type); } }
        public static string ValueName { get { return nameof(Edge.Value); } }

        private const string _sourceScriptType = $"Source{nameof(Edge.Source.ScriptType)}";
        private const string _sourceAddress = $"Source{nameof(Edge.Source.Address)}";
        private const string _targetScriptType = $"Target{nameof(Edge.Target.ScriptType)}";
        private const string _targetAddress = $"Target{nameof(Edge.Target.Address)}";
        private const string _blockHeight = nameof(Edge.BlockHeight);

        /// <summary>
        /// Note that the ordre of the items in this array should 
        /// match those in the `GetCsvHeader` method.
        /// </summary>
        private static readonly string[] _properties = new string[]
        {
            _sourceScriptType,
            _sourceAddress,
            _targetScriptType,
            _targetAddress,
            EdgeType,
            ValueName,
            _blockHeight
        };

        public override string GetLabels()
        {
            return Labels;
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
                edge.Source.ScriptType.ToString(), 
                edge.Source.Address,
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

            builder.Append(
                $"MERGE (source:{GetLabels()} {{" +
                $"{nameof(Edge.Source.ScriptType)}: line.{_sourceScriptType}," +
                $"{nameof(Edge.Source.Address)}: line.{_sourceAddress}" +
                $"}}) ");

            builder.Append(
                $"MERGE (target:{GetLabels()} {{" +
                $"{nameof(Edge.Target.ScriptType)}: line.{_targetScriptType}," +
                $"{nameof(Edge.Target.Address)}: line.{_targetAddress}" +
                $"}}) ");

            builder.Append("WITH source, target, line ");

            builder.Append(
                $"MATCH (block:{BlockBulkLoadMapper.Labels} {{" +
                $"{BlockBulkLoadMapper.HeightPropertyName}: line.{_blockHeight}" +
                $"}}) ");

            builder.Append(
                "CREATE (source)-[:Redeems]->(block) " +
                "CREATE (block)-[:Creates]->(target) " +
                "WITH source, target, line ");

            builder.Append(
                "CALL apoc.create.relationship(" +
                "source, " +
                $"line.{EdgeType}, {{}}, target) YIELD rel RETURN distinct 'done'");

            return builder.ToString();
        }
    }
}
