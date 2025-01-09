namespace BC2G.PersistentObject;

public class PersistentBlockAddresses(
    string filename,
    int maxObjectsPerFile,
    ILogger<PersistentBlockAddresses> logger,
    CancellationToken cT) :
    PersistentObject<string>(
        filename,
        maxObjectsPerFile,
        logger,
        cT)
{ }
