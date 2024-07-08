namespace BC2G.PersistentObject;

public class PersistentBlockAddressess(
    string filename,
    ILogger<PersistentBlockAddressess> logger,
    CancellationToken cT) :
    PersistentObject<string>(
        filename,
        logger,
        cT,
        BlockStatistics.GetHeaderAddresses())
{ }
