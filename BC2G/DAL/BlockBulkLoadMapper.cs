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
        public const string label = "Block";

        /// Note that the ordre of the items in this array should 
        /// match those returned from the `ToCsv()` method.. 
        private static readonly Prop[] _properties = new Prop[]
        {
            Prop.Height,
            Prop.BlockMedianTime,
            Prop.BlockConfirmations,
            Prop.BlockDifficulty,
            Prop.BlockTxCount,
            Prop.BlockSize,
            Prop.BlockStrippedSize,
            Prop.BlockWeight
        };

        public BlockBulkLoadMapper(
            string importPrefix,
            string importDirectory,
            string filename = "tmpBulkImportBlocks.csv") :
            base(importPrefix, importDirectory, filename)
        { }

        public override string GetCsvHeader()
        {
            return string.Join(
                csvDelimiter,
                from x in _properties select Props[x].CsvHeader);
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
                $"LOAD CSV WITH HEADERS FROM '{filename}' AS {Property.lineVarName} " +
                $"FIELDTERMINATOR '{csvDelimiter}' " +
                $"MERGE (b: {label} {{" +
                $"{Props[Prop.Height].GetLoadExp(":")}}})" +
                $"ON CREATE SET ");

            string comma = "";
            foreach (var p in _properties) if (p != Prop.Height)
                {
                    builder.Append($"{comma}b.{Props[p].GetLoadExp()}");
                    comma = ", ";
                }

            builder.Append(
                " ON MATCH SET " +
                $"b.{Props[Prop.BlockConfirmations].GetLoadExp()}");

            return builder.ToString();
        }
    }
}
