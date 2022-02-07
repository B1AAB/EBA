using BC2G.Graph;
using BC2G.Logging;
using BC2G.Serializers;
using System.Text;

namespace BC2G
{
    public class PersistentGraphBuffer : PersistentObject<BlockGraph>
    {
        private const string _delimiter = ",";

        private readonly AddressToIdMapper _mapper;
        private readonly PersistentBlockStatistics _pBlockStatistics;
        private readonly Logger _logger;

        public PersistentGraphBuffer(
            string filename,
            AddressToIdMapper mapper,
            PersistentBlockStatistics pBlockStatistics,
            Logger logger,
            CancellationToken cancellationToken) : base(
                filename,
                cancellationToken,
                string.Join(_delimiter, new string[]
                { "Source", "Target", "Value", "EdgeType", "Timestamp" }))
        {
            _mapper = mapper;
            _pBlockStatistics = pBlockStatistics;
            _logger = logger;
        }

        public override string Serialize(BlockGraph obj, CancellationToken cancellationToken)
        {
            obj.MergeQueuedTxGraphs(cancellationToken);

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

            obj.Stats.StopStopwatch();
            _pBlockStatistics.Enqueue(obj.Stats.ToString());

            return csvBuilder.ToString();
        }

        public override void PostPersistence(BlockGraph obj)
        {
            _logger.LogFinishProcessingBlock(
                obj.Height,
                _mapper.NodesCount,
                obj.EdgeCount,
                obj.Stats.Runtime.TotalSeconds);
        }
    }
}
