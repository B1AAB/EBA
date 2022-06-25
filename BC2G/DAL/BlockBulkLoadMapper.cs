using BC2G.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.DAL
{
    internal class BlockBulkLoadMapper : ModelMapper<Block>
    {
        public class Neo4jModel : Neo4jModelBase
        {
            public const string label = "Block";
            public const string medianTime = "MedianTime";
            public const string confirmations = "Confirmations";
            public const string difficulty = "Difficulty";
            public const string txCount = "TransactionsCount";
            public const string size = "Size";
            public const string strippedSize = "StrippedSize";
            public const string weight = "Weight";
        }

        /// Note that the ordre of the items in this array should 
        /// match those returned from the `ToCsv()` method.. 
        private static readonly string[] _properties = new string[]
        {
            Neo4jModel.height,
            Neo4jModel.medianTime,
            Neo4jModel.confirmations,
            Neo4jModel.difficulty,
            Neo4jModel.txCount,
            Neo4jModel.size,
            Neo4jModel.strippedSize,
            Neo4jModel.weight
        };

        public BlockBulkLoadMapper(
            string importPrefix,
            string importDirectory,
            string filename = "tmpBulkImportBlocks.csv") :
            base(importPrefix, importDirectory, filename)
        { }

        public override string GetCsvHeader()
        {
            return string.Join(csvDelimiter, _properties);
        }

        public override string ToCsv(Block block)
        {
            /// Note that the ordre of the items in this array should 
            /// match those in the `_properties`. 
            return string.Join(csvDelimiter, new string[]
            {
                block.Height.ToString(),
                block.MedianTime.ToString(),
                block.Confirmations.ToString(),
                block.Difficulty.ToString(),
                block.TransactionsCount.ToString(),
                block.Size.ToString(),
                block.StrippedSize.ToString(),
                block.Weight.ToString()
            });
        }

        protected override string ComposeCypherQuery(string filename)
        {
            var builder = new StringBuilder();
            builder.Append(
                $"LOAD CSV WITH HEADERS FROM '{filename}' AS line " +
                $"FIELDTERMINATOR '{csvDelimiter}' " +
                $"MERGE (b: {Neo4jModel.label} {{" +
                $"{Neo4jModel.height}: line.{Neo4jModel.height}}})" +
                $"ON CREATE SET ");

            string comma = "";
            foreach (var p in _properties) if (p != Neo4jModel.height)
                {
                    builder.Append($"{comma}b.{p}=line.{p}");
                    comma = ", ";
                }

            builder.Append(
                " ON MATCH SET " +
                $"b.{Neo4jModel.confirmations}=line.{Neo4jModel.confirmations}");

            return builder.ToString();
        }
    }
}
