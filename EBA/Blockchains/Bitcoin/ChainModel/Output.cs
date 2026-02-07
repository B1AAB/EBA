using EBA.Utilities;

namespace EBA.Blockchains.Bitcoin.ChainModel;

public class Output : IBase64Serializable
{
    [JsonPropertyName("value")]
    public double ValueBTC
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

    [JsonPropertyName("n")]
    public int Index { set; get; }

    [JsonPropertyName("scriptPubKey")]
    public ScriptPubKey ScriptPubKey { set; get; }

    public Output() { }
    public Output(long value, ScriptPubKey scriptPubKey)
    {
        Value = value;
        ScriptPubKey = scriptPubKey;
    }

    public bool TryGetAddress(out string? address)
    {
        address = ScriptPubKey.GetAddress();

        if (string.IsNullOrEmpty(address))
        {
            address = null;
            return false;
        }

        return true;
    }

    public bool IsValueTransfer
    {
        get
        {
            _isValueTransfer ??= Value > 0;
            return _isValueTransfer.Value;
        }
    }
    private bool? _isValueTransfer = null;

    public string ToBase64String()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(Value);
            writer.Write(Index);
            if (ScriptPubKey != null)
                writer.Write(ScriptPubKey.ToBase64String());
        }
        return Convert.ToBase64String(stream.ToArray());
    }
}
