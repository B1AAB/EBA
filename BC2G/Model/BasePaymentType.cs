namespace BC2G.Model;

public abstract class BasePaymentType
{
    public abstract ScriptType ScriptType { get; }

    public abstract string GetAddress();
}
