namespace BC2G.PersistentObject;

public class PersistentBlockAddressess(
    string filename,
    int maxObjectsPerFile,
    ILogger<PersistentBlockAddressess> logger,
    CancellationToken cT) :
    PersistentObject<string>(
        filename,
        maxObjectsPerFile,
        logger,
        cT)
{ }
