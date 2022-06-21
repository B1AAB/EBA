using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.DAL
{
    internal abstract class ModelMapper<T>
    {
        internal readonly char csvDelimiter;
        internal readonly char labelsDelimiter;

        public ModelMapper(char csvDelimiter = '\t', char labelsDelimiter = ':')
        {
            this.csvDelimiter = csvDelimiter;
            this.labelsDelimiter = labelsDelimiter;
        }

        /// <summary>
        /// Returns a list of labels delimited by <typeparamref name="csvDelimiter"/> 
        /// if more than one label.
        /// </summary>
        /// <returns></returns>
        public abstract string GetLabels();

        public abstract string GetCsvHeader();        
        public abstract string ToCsv(T obj);
        public abstract string GetCypherQuery(string filename);
    }
}
