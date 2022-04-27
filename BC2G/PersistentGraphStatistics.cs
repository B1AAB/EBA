using BC2G.Graph;

namespace BC2G
{
    public class PersistentGraphStatistics : PersistentObject<string>
    {
        public PersistentGraphStatistics(
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
