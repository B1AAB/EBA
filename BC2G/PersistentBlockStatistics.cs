using BC2G.Model;

namespace BC2G
{
    // TODO: ideally this should be implemented using BlockStatistics 
    // i.e., PersistentObject<BlockStatistics>. However, BlockStatistics 
    // is not currently immutable, hence, if an instance of BlockStatistics
    // is queued, there is not guarantee it is not modified while it is 
    // waiting in the queue to be persisted. So, first that type should be
    // made immutable, then this type changed to use the modified type.

    public class PersistentBlockStatistics : PersistentObject<string>
    {
        public PersistentBlockStatistics(
            string filename,
            CancellationToken cancellationToken) : base(
                filename,
                cancellationToken,
                BlockStatistics.GetHeader())
        { }

        public override string Serialize(
            string obj,
            CancellationToken cancellationToken)
        {
            return obj;
        }
    }
}
