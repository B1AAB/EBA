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
        public const string neo4jModelLabel = "Block";
        public const string neo4jModelHeight = "Height";
        public const string neo4jModelMedianTime = "MedianTime";
        public const string neo4jModelConfirmations = "Confirmations";
        public const string neo4jModelDifficulty = "Difficulty";
        public const string neo4jModelTxCount = "TransactionsCount";
        public const string neo4jModelSize = "Size";
        public const string neo4jModelStrippedSize = "StrippedSize";
        public const string neo4jModelWeight = "Weight";

        /// Note that the ordre of the items in this array should 
        /// match those returned from the `ToCsv()` method.. 
        private static readonly string[] _properties = new string[]
        {
            neo4jModelHeight,
            neo4jModelMedianTime,
            neo4jModelConfirmations,
            neo4jModelDifficulty,
            neo4jModelTxCount,
            neo4jModelSize,
            neo4jModelStrippedSize,
            neo4jModelWeight
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
                $"MERGE (: {neo4jModelLabel} {{");

            string comma = "";
            foreach (var p in _properties)
            {
                builder.Append($"{comma}{p}: line.{p}");
                comma = ", ";
            }

            builder.Append("})");
            return builder.ToString();
        }
    }
}
