namespace BC2G.PersistentObject;

public class PersistentTxoLifeCycleBuffer(
    string filename,
    ILogger<PersistentObject<Utxo>> logger,
    CancellationToken cT,
    string? header = null) :
    PersistentObject<Utxo>(filename, logger, cT, header)
{ }
