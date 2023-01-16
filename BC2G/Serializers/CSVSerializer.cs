namespace BC2G.Serializers
{
    public class CSVSerializer : SerializerBase, IDisposable
    {
        private const string _delimiter = "\t";
        private const string _tmpFilenamePostfix = ".tmp";
        private bool disposed = false;

        private readonly List<string> _createdFiles = new();
        //private readonly AddressToIdMapper _mapper;

        public CSVSerializer()//AddressToIdMapper mapper)
        {
            //_mapper = mapper;
        }

        public override void Serialize(BlockGraph g, string baseFilename)
        {
            var nodesFilename = baseFilename + "_nodes.tsv" + _tmpFilenamePostfix;
            var edgeFilename = baseFilename + "_edges.tsv" + _tmpFilenamePostfix;

            WriteNodes(g, nodesFilename);
            WriteEdges(g, edgeFilename);

            _createdFiles.Add(nodesFilename);
            _createdFiles.Add(edgeFilename);
        }

        public void Serialize(ConcurrentQueue<BlockGraph> graphsBuffer, string edgesFilename)
        {
            // TODO: Note that this approach is not using a staging 
            // file to first write to that, and then replace the 
            // staging file with the expected one (as is done by 
            // the other serializer methods). This mainly becuase this
            // approach appends to an existing file, hence if the 
            // existing file is very big, it will endup copying 
            // the big file to a temporary path, add a few lines
            // to the temporary file, then replace the original
            // file with the temporary file. This approach is not
            // efficient, hence it would need a better/optimized alternative.
            var csvBuilder = new StringBuilder();

            if (!File.Exists(edgesFilename))
                csvBuilder.AppendLine(string.Join(_delimiter, new string[]
                {
                    "Source", "Target", "Value", "EdgeType", "Timestamp"
                }));

            foreach (var g in graphsBuffer)
                foreach (var edge in g.Edges)
                    csvBuilder.AppendLine(
                        string.Join(_delimiter, new string[]
                        {
                            edge.Source.Id,
                            edge.Target.Id,
                            edge.Value.ToString(),
                            ((byte)edge.Type).ToString(),
                            edge.Timestamp.ToString()
                        }));
            
            File.AppendAllText(edgesFilename, csvBuilder.ToString());
        }

        public override BlockGraph Deserialize(string path, int blockHeight)
        {
            var nodeIds = ReadNodes(Path.Combine(path, $"{blockHeight}_nodes.tsv"));
            return ReadEdges(Path.Combine(path, $"{blockHeight}_edges.tsv"), blockHeight, nodeIds);
        }

        public static Dictionary<int, BlockGraph> Deserialize(string edges, string addressIdMapping)
        {
            var mappings = new Dictionary<string, string>();
            foreach (var l in File.ReadAllLines(addressIdMapping))
            {
                var cols = l.Trim().Split('\t');
                mappings.Add(cols[0], cols[1]);
            }

            var blockGraphs = new Dictionary<int, BlockGraph>();

            string? line;
            using var reader = new StreamReader(edges);
            reader.ReadLine(); // skip the header.
            while ((line = reader.ReadLine()) != null)
            {
                var cols = line.Trim().Split(_delimiter);
                var edge = Edge.FromString(cols, mappings[cols[0]], mappings[cols[1]]);

                if (!blockGraphs.ContainsKey(edge.BlockHeight))
                    blockGraphs.Add(edge.BlockHeight, new BlockGraph(edge.BlockHeight));

                blockGraphs[edge.BlockHeight].AddEdge(edge);
            }

            return blockGraphs;
        }

        private void WriteNodes(BlockGraph g, string filename)
        {
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine(
                string.Join(_delimiter, new string[]
                { "Id", "Label" }));

            foreach (var node in g.Nodes)
                csvBuilder.AppendLine(string.Join(_delimiter, new string[]
                {
                    node.Id, node.Address
                }));

            File.WriteAllText(filename, csvBuilder.ToString());
        }

        private static Dictionary<string, string> ReadNodes(string filename)
        {
            var nodeIds = new Dictionary<string, string>();
            using var reader = new StreamReader(filename);
            string? line;
            reader.ReadLine(); // skip the header.
            while ((line = reader.ReadLine()) != null)
            {
                var x = line.Split(_delimiter);
                nodeIds.Add(x[0], x[1]);
            }

            return nodeIds;
        }

        private void WriteEdges(BlockGraph g, string filename)
        {
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine(Edge.Header);

            foreach (var edge in g.Edges)
                csvBuilder.AppendLine(
                    edge.ToString(edge.Source.Id, edge.Target.Id));

            File.WriteAllText(filename, csvBuilder.ToString());
        }

        private static BlockGraph ReadEdges(string filename, int height, Dictionary<string, string> nodeIds)
        {
            var g = new BlockGraph(height);
            string? line;
            using var reader = new StreamReader(filename);
            reader.ReadLine(); // skip the header.
            while ((line = reader.ReadLine()) != null)
            {
                var x = line.Split(_delimiter);
                // TODO: fix setting node script type.
                g.AddEdge(new Edge(
                    new Node(x[0], nodeIds[x[0]], ScriptType.Unknown),
                    new Node(x[1], nodeIds[x[1]], ScriptType.Unknown),
                    double.Parse(x[2]),
                    (EdgeType)int.Parse(x[3]),
                    uint.Parse(x[4]),
                    int.Parse(x[5])));
            }

            return g;
        }

        // The IDisposable interface is implemented following .NET docs:
        // https://docs.microsoft.com/en-us/dotnet/api/system.idisposable?view=net-6.0
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    /// rename temporary files as the following.
                    ///  from: abc.tsv.tmp
                    ///    to: abc.tsv
                    foreach (var filename in _createdFiles)
                        File.Move(
                            filename,
                            filename[..filename.LastIndexOf(_tmpFilenamePostfix)],
                            true);
                }

                disposed = true;
            }
        }
    }
}
