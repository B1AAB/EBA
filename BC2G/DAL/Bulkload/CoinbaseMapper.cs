namespace BC2G.DAL.Bulkload
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
            string workingDirectory,
            string cypherImportPrefix,
            //string importDirectory,
            string filename = "tmpBulkImportCoinbase.csv") :
            base(workingDirectory, cypherImportPrefix, /*importDirectory,*/ filename)
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
            var l = Property.lineVarName;

            return
                $"LOAD CSV WITH HEADERS FROM '{filename}' AS {l} " +
                $"FIELDTERMINATOR '{csvDelimiter}' " +
                $"MATCH (coinbase:{BitcoinAgent.Coinbase}) " +
                $"MERGE (target:{labels} {{" +
                $"{Props[Prop.EdgeTargetAddress].GetLoadExp(":")}}}) " +
                $"SET target.{Props[Prop.EdgeTargetType].GetLoadExp("=")} " +
                $"WITH coinbase, target, {l} " +
                $"MATCH (block:{BlockMapper.label} {{" +
                $"{Props[Prop.Height].GetLoadExp(":")}" +
                $"}}) " +
                // Create edge between the script and its corresponding block
                CreatesEdgeQuery +
                $"WITH coinbase, target, {l} " +
                // Create edge between the coinbase node and the script
                $"CALL apoc.merge.relationship (" +
                $"coinbase, " + // source
                $"{l}.{Props[Prop.EdgeType].CsvHeader}, " + // relationship type
                $"{{" + // properties
                $"{Props[Prop.EdgeValue].GetLoadExp(":")}, " +
                $"{Props[Prop.Height].GetLoadExp(":")}" +
                $"}}, " +
                $"{{ Count : 0 }}, " + // on create
                $"target, " + // target
                $"{{}}" + // on update
                $")" +
                $"YIELD rel " +
                $"SET rel.Count = rel.Count + 1 " +
                $"RETURN distinct 'DONE'";
        }
    }
}
