using BC2G.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.DAL
{
    internal abstract class ModelMapper<T>
    {
        public const string csvDelimiter = "\t";
        public const string labelsDelimiter = ":";
        public const string lineVarName = "line";

        private const string _addressProperty = "Address";
        private const string _scriptTypeProperty = "ScriptType";

        public static Dictionary<Prop, Property> Props = new()
        {
            {Prop.Height, new Property("Height", FieldType.Int) },
            {Prop.ScriptAddress, new Property(_addressProperty) },
            {Prop.ScriptType, new Property(_scriptTypeProperty) },
            {Prop.BlockMedianTime, new Property("MedianTime")},
            {Prop.BlockConfirmations, new Property("Confirmations", FieldType.Int) },
            {Prop.BlockDifficulty, new Property("Difficulty" , FieldType.Float)},
            {Prop.BlockTxCount, new Property("TransactionsCount", FieldType.Int) },
            {Prop.BlockSize, new Property("Size", FieldType.Int) },
            {Prop.BlockStrippedSize, new Property("StrippedSize")},
            {Prop.BlockWeight, new Property("Weight", FieldType.Int) },
            {Prop.EdgeSourceAddress, new Property(_addressProperty, csvHeader: "SourceAddress") },
            {Prop.EdgeSourceType, new Property(_scriptTypeProperty, csvHeader: "SourceType") },
            {Prop.EdgeTargetAddress, new Property(_addressProperty, csvHeader: "DestAddress") },
            {Prop.EdgeTargetType, new Property(_scriptTypeProperty, csvHeader: "DestType") },
            {Prop.EdgeType, new Property("EdgeType") },
            {Prop.EdgeValue, new Property("Value", FieldType.Float) }
        };

        public string Filename { get; }
        public string CypherQuery { get; }

        public ModelMapper(
            string cypherImportPrefix,
            string importDirectory,
            string filename)
        {
            Filename = Path.Combine(importDirectory, filename);
            CypherQuery = ComposeCypherQuery(cypherImportPrefix + filename);
        }

        public abstract string GetCsvHeader();
        public abstract string ToCsv(T obj);
        protected abstract string ComposeCypherQuery(string filename);
    }
}
