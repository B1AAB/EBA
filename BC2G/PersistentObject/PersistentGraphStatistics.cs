namespace BC2G.PersistentObject;

public class PersistentGraphStatistics : PersistentObject<string>
{
    public PersistentGraphStatistics(
        string filename, ILogger<PersistentGraphStatistics> logger, CancellationToken cT) :
        base(filename, logger, cT, BlockStatistics.GetHeader())
    { }
}
