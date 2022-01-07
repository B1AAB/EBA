using BC2G.Graph;
using System.Text;

namespace BC2G.Serializers
{
    public class CSVSerializer : SerializerBase, IDisposable
    {
        private const string _delimiter = ",";
        private const string _tmpFilenamePostfix = ".tmp";
        private bool disposed = false;

        private readonly string _mapperFilename = string.Empty;
        private readonly List<string> _createdFiles = new();
        private readonly AddressToIdMapper _mapper = new();

        public CSVSerializer() { }

        public CSVSerializer(AddressToIdMapper mapper)
        {
            _mapper = mapper;
        }

        public override void Serialize(GraphBase g, string baseFilename)
        {
            var nodesFilename = baseFilename + "_nodes.csv" + _tmpFilenamePostfix;
            var edgeFilename = baseFilename + "_edges.csv" + _tmpFilenamePostfix;

            WriteNodes(g, nodesFilename);
            WriteEdges(g, edgeFilename);

            _createdFiles.Add(nodesFilename);
            _createdFiles.Add(edgeFilename);
        }

        public override GraphBase Deserialize(string path, int blockHeight)
        {
            var nodeIds = ReadNodes(Path.Combine(path, $"{blockHeight}_nodes.csv"));
            return ReadEdges(Path.Combine(path, $"{blockHeight}_edges.csv"), nodeIds);
        }

        private void WriteNodes(GraphBase g, string filename)
        {
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine(
                string.Join(_delimiter, new string[]
                {
                    "Id", "Label"
                }));

            foreach (var node in g.Nodes)
                csvBuilder.AppendLine(string.Join(_delimiter, new string[]
                {
                    _mapper.GetId(node).ToString(),
                    node
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

        private void WriteEdges(GraphBase g, string filename)
        {
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine(
                string.Join(_delimiter, new string[]
                {
                    "Source", "Target", "Weight", "EdgeType" // Do not name this as "Type"
                }));

            foreach (var edge in g.Edges)
                csvBuilder.AppendLine(
                    string.Join(_delimiter, new string[]
                    {
                        _mapper.GetId(edge.Source).ToString(),
                        _mapper.GetId(edge.Target).ToString(),
                        edge.Value.ToString(),
                        ((byte)edge.Type).ToString()
                    }));

            File.WriteAllText(filename, csvBuilder.ToString());
        }

        private static GraphBase ReadEdges(string filename, Dictionary<string, string> nodeIds)
        {
            var g = new GraphBase();

            using var reader = new StreamReader(filename);
            string? line;
            string[] x;
            reader.ReadLine(); // skip the header.
            while ((line = reader.ReadLine()) != null)
            {
                x = line.Split(_delimiter);
                g.AddEdge(new Model.Edge(
                    nodeIds[x[0]],
                    nodeIds[x[1]],
                    double.Parse(x[2]),
                    (Model.EdgeType)int.Parse(x[3])));
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
                    /// rename temporary files since the 
                    /// method is executed successfully.
                    /// Rename pattern:
                    ///     from: abc.csv.tmp
                    ///       to: abc.csv
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
