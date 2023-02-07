namespace BC2G.PersistentObject;

public class PersistentGraphStatistics : PersistentObject<string>
{
    public PersistentGraphStatistics(
        string filename, CancellationToken cT) :
        base(filename, cT, BlockStatistics.GetHeader())
    { }
}
