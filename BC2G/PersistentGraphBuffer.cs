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
        private readonly PersistentGraphStatistics _pGraphStats;
        private readonly Logger _logger;

        public PersistentGraphBuffer(
            string filename,
            AddressToIdMapper mapper,
            PersistentGraphStatistics pGraphStats,
            Logger logger,
            CancellationToken cancellationToken) : base(
                filename,
                cancellationToken,
                Edge.Header)
        {
            _mapper = mapper;
            _pGraphStats = pGraphStats;
            _logger = logger;
        }

        public override string Serialize(BlockGraph obj, CancellationToken cancellationToken)
        {
            obj.MergeQueuedTxGraphs(cancellationToken);

            var csvBuilder = new StringBuilder();
            foreach (var edge in obj.Edges)
                csvBuilder.AppendLine(
                    edge.ToString(
                        _mapper.GetId(edge.Source),
                        _mapper.GetId(edge.Target)));

            obj.Stats.StopStopwatch();
            _pGraphStats.Enqueue(obj.Stats.ToString());

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
