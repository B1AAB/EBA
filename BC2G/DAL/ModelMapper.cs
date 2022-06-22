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
