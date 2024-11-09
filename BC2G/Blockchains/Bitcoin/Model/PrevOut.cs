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

    public Output ConstructedOutput { init;  get; }

    public PrevOut()
    {
        ConstructedOutput = new Output()
        {
            ScriptPubKey = ScriptPubKey
        };
    }
}
