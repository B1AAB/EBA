using BC2G.Graph;

namespace BC2G
{
    public class PersistentBlockStatistics : PersistentObject<string>
    {
        public PersistentBlockStatistics(
            string filename,
            CancellationToken cancellationToken) : base(
                filename,
                cancellationToken,
                GraphStatistics.GetHeader())
        { }

        public override string Serialize(
            string obj,
            CancellationToken cancellationToken)
        {
            return obj;
        }
    }
}
