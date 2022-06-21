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
        public static readonly string Labels = "Block";
        public static readonly string HeightPropertyName = nameof(Block.Height);

        /// <summary>
        /// Note that the ordre of the items in this array should 
        /// match those in the `GetCsvHeader` method.
        /// </summary>
        private static readonly string[] _properties = new string[]
        {
            HeightPropertyName,
            nameof(Block.MedianTime),
            nameof(Block.Confirmations),
            nameof(Block.Difficulty),
            nameof(Block.TransactionsCount),
            nameof(Block.Size),
            nameof(Block.StrippedSize),
            nameof(Block.Weight)
        };

        public override string GetLabels()
        {
            return Labels;
        }

        public override string GetCsvHeader()
        {
            return string.Join(csvDelimiter, _properties);
        }

        public override string ToCsv(Block block)
        {
            /// Note that the order in this array 
            /// should match those in `_properties`. 
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

        public override string GetCypherQuery(string filename)
        {
            var builder = new StringBuilder();
            builder.Append(
                $"LOAD CSV WITH HEADERS FROM '{filename}' AS line " +
                $"FIELDTERMINATOR '{csvDelimiter}' " +
                $"MERGE (: {GetLabels()} {{");

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
