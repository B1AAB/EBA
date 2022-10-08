using BC2G.DAL;
using BC2G.Graph;
using BC2G.Logging;
using BC2G.Serializers;
using Microsoft.Extensions.Logging;

namespace BC2G.PersistentObject
{
    public class PersistentGraphBuffer : PersistentObject<BlockGraph>, IDisposable
    {
        //private readonly AddressToIdMapper _mapper;
        private readonly PersistentGraphStatistics _pGraphStats;
        private readonly ILogger<PersistentGraphBuffer> _logger;

        private readonly GraphDB _graphDB;

        private bool _disposed = false;

        public PersistentGraphBuffer(
            GraphDB graphdb,
            ILogger<PersistentGraphBuffer> logger,
            //string nodesFilename,
            //string edgesFilename,
            //AddressToIdMapper mapper,
            PersistentGraphStatistics pGraphStats,
            //Logger logger,
            CancellationToken cancellationToken) : base(
                //logger,
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
            _logger.LogInformation("Finished processing block {height} in {runtime}.", obj.Height, obj.Stats.Runtime);
            /*_logger.LogFinishProcessingBlock(
                obj.Height,
                obj.Stats.Runtime.TotalSeconds);*/
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _graphDB?.FinishBulkImport();
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
