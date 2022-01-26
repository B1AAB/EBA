using BC2G.Graph;
using BC2G.Serializers;
using System.Text;

namespace BC2G
{
    public class PersistentGraphBuffer : PersistentObject<GraphBase>
    {
        private const string _delimiter = ",";

        private readonly AddressToIdMapper _mapper;

        public PersistentGraphBuffer(
            string filename,
            AddressToIdMapper mapper,
            CancellationToken cancellationToken) : base(
                filename,
                cancellationToken,
                string.Join(_delimiter, new string[]
                { "Source", "Target", "Value", "EdgeType", "Timestamp" }))
        {
            _mapper = mapper;
        }

        public override string Serialize(GraphBase obj, CancellationToken cancellationToken)
        {
            obj.MergeQueuedTxGraphs(cancellationToken);

            var csvBuilder = new StringBuilder();
            foreach (var edge in obj.Edges)
                csvBuilder.AppendLine(
                    string.Join(_delimiter, new string[]
                    {
                        _mapper.GetId(edge.Source).ToString(),
                        _mapper.GetId(edge.Target).ToString(),
                        edge.Value.ToString(),
                        ((byte)edge.Type).ToString(),
                        edge.Timestamp.ToString()
                    }));

            return csvBuilder.ToString();
        }
    }
}
