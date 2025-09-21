namespace BC2G.PersistentObject;

public class PersistentTxoLifeCycleBuffer(
    string filename,
    int maxTxoPerFile,
    ILogger<PersistentObject<Utxo>> logger,
    CancellationToken cT,
    string? header = null) :
    PersistentObject<Utxo>(filename, maxTxoPerFile, logger, cT, header)
{ }
