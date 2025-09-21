namespace BC2G.PersistentObject;

public class PersistentGraphStatistics(
    string filename,
    int maxObjectsPerFile,
    ILogger<PersistentGraphStatistics> logger,
    CancellationToken cT) :
    PersistentObject<string>(
        filename,
        maxObjectsPerFile,
        logger,
        cT,
        BlockStatistics.GetHeader())
{ }
