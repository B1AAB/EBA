namespace BC2G.Blockchains.Bitcoin.Model;

public class PrevOut
{
    [JsonPropertyName("generated")]
    public bool Generated { set; get; }

    [JsonPropertyName("height")]
    public int Height { set; get; }

    [JsonPropertyName("value")]
    public double Value { set; get; }

    [JsonPropertyName("scriptPubKey")]
    public ScriptPubKey? ScriptPubKey { set; get; }

    public Output ConstructedOutput
    {
        get
        {
            return new Output()
            {
                Value = Value,
                ScriptPubKey = ScriptPubKey
            };
        }
    }
}
