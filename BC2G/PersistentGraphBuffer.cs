using BC2G.DAL;
using BC2G.Graph;
using BC2G.Logging;
using BC2G.Serializers;

namespace BC2G
{
    public class PersistentGraphBuffer : PersistentObject<BlockGraph>
    {
        //private readonly AddressToIdMapper _mapper;
        private readonly PersistentGraphStatistics _pGraphStats;
        private readonly Logger _logger;

        private readonly GraphDB _graphDB;

        public PersistentGraphBuffer(
            GraphDB graphdb,
            //string nodesFilename,
            //string edgesFilename,
            //AddressToIdMapper mapper,
            PersistentGraphStatistics pGraphStats,
            Logger logger,
            CancellationToken cancellationToken) : base(
                //nodesFilename,
                //edgesFilename,
                cancellationToken/*,
                Node.Header,
                Edge.Header*/)
        {
            _graphDB = graphdb;
            //_mapper = mapper;
            _pGraphStats = pGraphStats;
            _logger = logger;
        }

        public override string Serialize(
            BlockGraph obj,
            //StreamWriter nodesStream,
            //StreamWriter edgesStream,
            CancellationToken cT)
        {
            try
            {
                obj.MergeQueuedTxGraphs(cT);
                _graphDB.BulkImport(obj, cT);

                obj.Stats.StopStopwatch();
                _pGraphStats.Enqueue(obj.Stats.ToString());
                return string.Empty;
            }
            catch (OperationCanceledException) { return string.Empty; }
        }

        public override void PostPersistence(BlockGraph obj)
        {
            _logger.LogFinishProcessingBlock(
                obj.Height,
                1, // TODO: fixme.  //_mapper.NodesCount,
                obj.EdgeCount,
                obj.Stats.Runtime.TotalSeconds);
        }
    }
}
