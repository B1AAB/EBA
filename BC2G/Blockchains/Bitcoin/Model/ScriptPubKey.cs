using NBitcoin;

namespace BC2G.Blockchains.Bitcoin.Model;

public class ScriptPubKey : BasePaymentType, IBase64Serializable
{
    [JsonPropertyName("asm")]
    public string Asm { get; set; } = string.Empty;

    [JsonPropertyName("hex")]
    public string Hex { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address
    {
        set { _address = value; }
        get
        {
            if (!string.IsNullOrEmpty(_address))
                return _address;

            var parsedHex = Script.FromHex(Hex);
            BitcoinAddress? address = null;
            if (parsedHex.IsScriptType(NBitcoin.ScriptType.P2PKH))
            {
                address = parsedHex.GetDestinationAddress(Network.Main);
            }
            else if (parsedHex.IsScriptType(NBitcoin.ScriptType.P2PK))
            {
                var pubkeys = parsedHex.GetDestinationPublicKeys();
                address = pubkeys[0].GetAddress(ScriptPubKeyType.Legacy, Network.Main);
            }

            if (address != null)
                _address = address.ToString();
            else
            {
                throw new Exception("Check me");
            }

            return _address;
        }
    }
    private string _address = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    public override ScriptType ScriptType
    {
        get
        {
            return
                Enum.TryParse(
                    Type, ignoreCase:true, 
                    out ScriptType scriptType)
                ? scriptType : ScriptType.Unknown;
        }
    }

    public override string GetAddress()
    {
        return Address;
    }

    public string ToBase64String()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(Asm);
            writer.Write(Hex);
            writer.Write(Address);
            writer.Write(Type);
        }
        return Convert.ToBase64String(stream.ToArray());
    }
}
