namespace BC2G.Blockchains.Bitcoin.Model;

public class Transaction
{
    [JsonPropertyName("blockhash")]
    public string BlockHash { set; get; } = string.Empty;

    [JsonPropertyName("txid")]
    public string Txid { set; get; } = string.Empty;

    [JsonPropertyName("hash")]
    public string Hash { set; get; } = string.Empty;
    
    [JsonPropertyName("version")]
    public ulong Version { set; get; }
    
    [JsonPropertyName("size")]
    public int Size { set; get; }
    
    [JsonPropertyName("vsize")]
    public int VSize { set; get; }
    
    [JsonPropertyName("weight")]
    public int Weight { set; get; }
    
    [JsonPropertyName("locktime")]
    public long LockTime { set; get; }

    [JsonPropertyName("fee")]
    public double Fee { set; get; }

    [JsonPropertyName("vin")]
    public List<Input> Inputs { set; get; } = new List<Input>();

    [JsonPropertyName("vout")]
    public List<Output> Outputs { set; get; } = new List<Output>();

    public bool IsCoinbase
    {
        get
        {
            if (_isCoinbase == null)
                _isCoinbase = Inputs.Any(
                    x => !string.IsNullOrEmpty(x.Coinbase));

            return (bool)_isCoinbase;
        }
    }
    private bool? _isCoinbase = null;
}
