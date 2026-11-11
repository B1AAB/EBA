using NBitcoin;

namespace AAB.EBA.Blockchains.Bitcoin.ChainModel;

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
    public string Hex
    {
        set
        {
            _hex = value;
            _script = Script.FromHex(_hex);

            _hexBase58 = EBA.Utilities.Encoder.Base58(Convert.FromHexString(_hex));

            byte[] rawBytes = Convert.FromHexString(_hex);
            byte[] hashBytes = SHA256.HashData(rawBytes);
            _sha256HashBase58 = EBA.Utilities.Encoder.Base58(hashBytes);
        }
        get { return _hex; }
    }
    private string _hex = string.Empty;
    private Script _script = Script.Empty;

    public string HexBase58 { get { return _hexBase58; } }
    private string _hexBase58 = string.Empty;

    public string SHA256HashBase58 { get { return _sha256HashBase58; } }
    private string _sha256HashBase58 = string.Empty;

    [JsonPropertyName("address")]
    public string Address
    {
        set { _address = value; }
        get
        {
            if (!string.IsNullOrEmpty(_address))
                return _address;

            _address = ExtractOrBuildAddress();

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
                    Type, 
                    ignoreCase: true,
                    out ScriptType scriptType)
                ? scriptType : ScriptType.Unknown;
        }
    }

    public bool IsMalleable
    {
        get { return _script.IsMalleable; }
    }    

    public override string GetAddress()
    {
        return Address;
    }

    /// <summary>
    /// The goal is to know which Tx outputs (utxo) are paid to which key-pairs. 
    /// So, we're building an id that tries to identify the key-pair as best possible. 
    /// In many cases, address is enough and can be easily extracted from the locking script (scriptPubKey).
    /// But there are cases where address cannot be extracted, for instance, null-data scripts. 
    /// So, for any case that we cannot extract the address, we build one aiming for a unique id for the key-pair, or a unique id for the locking script.
    /// The alternative to this method is using a hash of the locking script as the id, and leave address strictly as the address extracted from the locking script; 
    /// but an important downside of that is two different locking scripts that pay to the same key-pair will have different ids, 
    /// and so tracking the flow of funds based on the key-pair will be more difficult.
    /// 
    /// TODO: also note that given that this will lead to different payments to the same key-pair having same ids, 
    /// it means one node will correspond to multiple locking scripts. currently only one locking script per node is kept, 
    /// but that should be extended to keep a list, such that you know different script types and locking mechanisims that pay to the same key-pair.
    /// </summary>
    /// <returns></returns>
    private string ExtractOrBuildAddress()
    {
        // Both Hex and Type are needed to extract or build the address:
        // ScriptType is derived from Type, and the made-up address uses the hash of Hex.
        // Fail instead of returning (and caching) an incomplete address, e.g. "Unknown-".
        // Note: _sha256HashBase58 is empty only when the Hex setter has never run;
        // an empty script (Hex = "") still gets the non-empty hash of zero bytes.
        if (string.IsNullOrEmpty(_sha256HashBase58) || string.IsNullOrEmpty(Type))
            throw new InvalidOperationException(
                $"Cannot extract or build the address before both " +
                $"{nameof(Hex)} and {nameof(Type)} are set.");

        BitcoinAddress? address;

        if (ScriptType == ScriptType.nonstandard || ScriptType == ScriptType.NullData)
        {
            // examples: 
            // non-standard:  71036
            // nulldata: 325001
            address = null;
        }
        else if (_script.IsScriptType(NBitcoin.ScriptType.P2PKH))
        {
            address = _script.GetDestinationAddress(Network.Main);
        }
        else if (_script.IsScriptType(NBitcoin.ScriptType.P2PK))
        {
            var pubkeys = _script.GetDestinationPublicKeys();
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

                address = null;
            }
        }
        else if (_script.IsScriptType(NBitcoin.ScriptType.MultiSig))
        {
            // example: 164467
            address = null;
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

            address = _script.GetDestinationAddress(Network.Main);
        }

        // example height where address is still null here from the P2PK's if condition: 840000

        var x = address?.ToString();
        if (!string.IsNullOrEmpty(x))
            return x;
        else
            return $"{ScriptType}-{_sha256HashBase58}";
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

    public override int GetHashCode()
    {
        return HashCode.Combine(_hex);
    }
}
