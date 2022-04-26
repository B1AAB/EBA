using BC2G.Graph;
using BC2G.Logging;
using BC2G.Serializers;
using System.Text;

namespace BC2G
{
    public class PersistentGraphBuffer : PersistentObject<BlockGraph>
    {
        private readonly AddressToIdMapper _mapper;
        private readonly PersistentGraphStatistics _pGraphStats;
        private readonly Logger _logger;

        public PersistentGraphBuffer(
            string nodesFilename,
            string edgesFilename,
            AddressToIdMapper mapper,
            PersistentGraphStatistics pGraphStats,
            Logger logger,
            CancellationToken cancellationToken) : base(
                nodesFilename,
                edgesFilename,
                cancellationToken,
                Node.Header,
                Edge.Header)
        {
            _mapper = mapper;
            _pGraphStats = pGraphStats;
            _logger = logger;
        }

        public override void Serialize(
            BlockGraph obj, 
            StreamWriter nodesStream, 
            StreamWriter edgesStream, 
            CancellationToken cT)
        {
            obj.MergeQueuedTxGraphs(cT);

            var edgesStringBuilder = new StringBuilder();
            foreach (var edge in obj.Edges)
                edgesStringBuilder.AppendLine(
                    edge.ToString(
                        edge.Source.Id,
                        edge.Target.Id));

            var nodesStringBuilder = new StringBuilder();
            foreach (var node in obj.Nodes)
                nodesStringBuilder.AppendLine(node.ToString());

            obj.Stats.StopStopwatch();
            _pGraphStats.Enqueue(obj.Stats.ToString());

            edgesStream.Write(edgesStringBuilder.ToString());
            nodesStream.Write(nodesStringBuilder.ToString());
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
