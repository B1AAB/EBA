using BC2G.Utilities;

namespace BC2G.Blockchains.Bitcoin.Model;

public class PrevOut
{
    [JsonPropertyName("generated")]
    public bool Generated { set; get; }

    [JsonPropertyName("height")]
    public int Height { set; get; }

    [JsonPropertyName("value")]
    public double RawValue
    {
        get { return _valueBTC; }
        set
        {
            _valueBTC = value;
            Value = Helpers.BTC2Satoshi(value);
        }
    }
    private double _valueBTC;

    public long Value { get; private set; }

    [JsonPropertyName("scriptPubKey")]
    public ScriptPubKey? ScriptPubKey { set; get; }

    public Output ConstructedOutput
    {
        get
        {
            return new Output(Value, ScriptPubKey);
        }
    }
}
