using NBitcoin;

namespace BC2G.Blockchains.Bitcoin.Model;

public class ScriptPubKey : BasePaymentType, IBase64Serializable
{
    [JsonPropertyName("asm")]
    public string Asm { set; get; } = string.Empty;

    /// <summary>
    /// Output Descriptor, docs:
    /// https://github.com/bitcoin/bitcoin/blob/master/doc/descriptors.md
    /// </summary>
    [JsonPropertyName("desc")]
    public string Descriptor { set; get; } = string.Empty;

    [JsonPropertyName("hex")]
    public string Hex { set; get; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address
    {
        set { _address = value; }
        get
        {
            if (!string.IsNullOrEmpty(_address))
                return _address;

            if (Type == "nonstandard")
            {
                _address = string.Empty;
                return _address;
            }

            _address = ExtractAddress();

            return _address;
        }
    }
    private string _address = string.Empty;

    [JsonPropertyName("type")]
    public string Type { set; get; } = string.Empty;

    public override ScriptType ScriptType
    {
        get
        {
            return
                Enum.TryParse(
                    Type, ignoreCase: true,
                    out ScriptType scriptType)
                ? scriptType : ScriptType.Unknown;
        }
    }

    public override string GetAddress()
    {
        return Address;
    }

    private string ExtractAddress()
    {
        var parsedHex = Script.FromHex(Hex);
        BitcoinAddress? address;

        if (parsedHex.IsScriptType(NBitcoin.ScriptType.P2PKH))
        {
            address = parsedHex.GetDestinationAddress(Network.Main);
        }
        else if (parsedHex.IsScriptType(NBitcoin.ScriptType.P2PK))
        {
            var pubkeys = parsedHex.GetDestinationPublicKeys();
            if (pubkeys.Length == 1)
            {
                address = pubkeys[0].GetAddress(ScriptPubKeyType.Legacy, Network.Main);
            }
            else
            {
                // Length = 0
                // Example:
                //   Height=292744,
                //   HEX="410400000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000ac"
                // 
                return string.Empty;
            }
        }
        else if (parsedHex.IsScriptType(NBitcoin.ScriptType.MultiSig))
        {
            return string.Empty;
        }
        else
        {
            // This condition is not expected to happen when interfacing
            // the Main network work using Bitcoin Core, because it already returns
            // address for these types.
            //
            // If happens, it may only happen for the following script types:
            // NBitcoin.ScriptType.P2SH
            // NBitcoin.ScriptType.P2WSH
            // NBitcoin.ScriptType.P2WPKH
            // NBitcoin.ScriptType.P2WSH
            // NBitcoin.ScriptType.Taproot
            // NBitcoin.ScriptType.P2SH
            // NBitcoin.ScriptType.Witness

            address = parsedHex.GetDestinationAddress(Network.Main);
        }

        if (address != null)
            return address.ToString();
        else
            throw new Exception($"Cannot extract destination address unexpectedly. HEX: {Hex}");
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
