namespace EBA.Blockchains.Bitcoin.ChainModel;

public abstract class BasePaymentType
{
    public abstract ScriptType ScriptType { get; }

    public abstract string GetAddress();
}
