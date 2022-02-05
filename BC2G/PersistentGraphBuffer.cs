using BC2G.Graph;
using BC2G.Serializers;
using System.Text;

namespace BC2G
{
    public class PersistentGraphBuffer : PersistentObject<GraphBase>
    {
        private const string _delimiter = ",";

        private readonly AddressToIdMapper _mapper;
        private readonly PersistentBlockStatistics _pBlockStatistics;

        public PersistentGraphBuffer(
            string filename,
            AddressToIdMapper mapper,
            PersistentBlockStatistics pBlockStatistics,
            CancellationToken cancellationToken) : base(
                filename,
                cancellationToken,
                string.Join(_delimiter, new string[]
                { "Source", "Target", "Value", "EdgeType", "Timestamp" }))
        {
            _mapper = mapper;
            _pBlockStatistics = pBlockStatistics;
        }

        public override string Serialize(GraphBase obj, CancellationToken cancellationToken)
        {
            obj.MergeQueuedTxGraphs(cancellationToken);
            _pBlockStatistics.Enqueue(obj.BlockStatistics.ToString());

            var csvBuilder = new StringBuilder();
            foreach (var edge in obj.Edges)
                csvBuilder.AppendLine(
                    string.Join(_delimiter, new string[]
                    {
                        _mapper.GetId(edge.Source),
                        _mapper.GetId(edge.Target),
                        edge.Value.ToString(),
                        ((byte)edge.Type).ToString(),
                        edge.Timestamp.ToString()
                    }));

            return csvBuilder.ToString();
        }
    }
}
