namespace BC2G.Blockchains.Bitcoin.Model;

public abstract class BasePaymentType
{
    public abstract ScriptType ScriptType { get; }

    public abstract string GetAddress();
}
