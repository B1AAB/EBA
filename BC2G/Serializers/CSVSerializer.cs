using BC2G.Graph;
using System.Text;

namespace BC2G.Serializers
{
    internal class CSVSerializer : BaseSerializer
    {
        private const string _delimiter = ",";

        public CSVSerializer(string addressToIdMappingsFilename) :
            base(addressToIdMappingsFilename)
        { }

        public override void Serialize(BlockGraph g, string baseFilename)
        {
            WriteNodes(g, baseFilename + "_nodes.csv");
            WriteEdges(g, baseFilename + "_edges.csv");
        }

        private void WriteNodes(BlockGraph g, string filename)
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
                    Mapper.GetId(node).ToString(),
                    node
                }));

            File.WriteAllText(filename, csvBuilder.ToString());
        }

        private void WriteEdges(BlockGraph g, string filename)
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
                        Mapper.GetId(edge.Source).ToString(),
                        Mapper.GetId(edge.Target).ToString(),
                        edge.Value.ToString(),
                        ((byte)edge.Type).ToString()
                    }));

            File.WriteAllText(filename, csvBuilder.ToString());
        }
    }
}
