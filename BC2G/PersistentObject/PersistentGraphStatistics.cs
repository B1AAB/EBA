using BC2G.Graph;
using BC2G.Logging;

namespace BC2G.PersistentObject
{
    public class PersistentGraphStatistics : PersistentObject<string>
    {
        public PersistentGraphStatistics(
            string filename,
            Logger logger,
            CancellationToken cancellationToken) : base(
                filename,
                logger,
                cancellationToken,
                BlockStatistics.GetHeader())
        { }
    }
}
