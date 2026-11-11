namespace AAB.EBA.Blockchains.Bitcoin.GraphModel;

public class ScriptNode : Node, IComparable<ScriptNode>, IEquatable<ScriptNode>
{
    public new static NodeKind Kind => NodeKind.Script;
    public override NodeKind NodeKind => Kind;    

    public string Address { get; }

    public ScriptType ScriptType { get; }

    public string? HexBase64 { get; }

    /// <summary>
    /// TODO: this is a feature to add if we needed to look up for addresses based on the locking script hash.
    /// 
    /// This is a list of SHA256 has of the HEX string of different 
    /// locking scripts paid to the keypair identified by the Address property.
    /// Most this set will contain only one item, but in some cases, 
    /// there may be more than one locking script paid to the same keypair.
    /// Using hashset to avoid duplicates. 
    /// 
    /// One example where one address can have multiple locking scripts is the following:
    /// 
    /// tx 1: b0a478fc79ecca1173cefe5975dd4a9874593cceb44b66ea4ea14eb40d587a14
    ///     "scriptPubKey": {
    ///         "asm": "043bbe78a34509d40c4728692c2b079931b2ee2c448baa2df7427dc0e489efcd58423c6e1a55cd91095387f7e688a5db7360760c1a583e589a3135e8e2f95627d8 OP_CHECKSIG",
    ///         "desc": "pk(043bbe78a34509d40c4728692c2b079931b2ee2c448baa2df7427dc0e489efcd58423c6e1a55cd91095387f7e688a5db7360760c1a583e589a3135e8e2f95627d8)#8739e2rf",
    ///         "hex": "41043bbe78a34509d40c4728692c2b079931b2ee2c448baa2df7427dc0e489efcd58423c6e1a55cd91095387f7e688a5db7360760c1a583e589a3135e8e2f95627d8ac",
    ///         "type": "pubkey"
    ///     }
    /// 
    /// tx 2: 020884874ee2cee8c8e275c9efd6369dedc08a458ccb437eabd1feae7672ef68
    ///     {
    ///        "value": 0.00000547,
    ///        "n": 214,
    ///        "scriptPubKey": {
    ///            "asm": "OP_DUP OP_HASH160 00008fdfba666df4490cf797b3643655e08df8f8 OP_EQUALVERIFY OP_CHECKSIG",
    ///            "desc": "addr(111g3qjk7YzhdkKuMmTBzUB8gQ55YZ6MG)#kq4du66h",
    ///            "hex": "76a91400008fdfba666df4490cf797b3643655e08df8f888ac",
    ///            "address": "111g3qjk7YzhdkKuMmTBzUB8gQ55YZ6MG",
    ///            "type": "pubkeyhash"
    ///         }
    ///     }
    /// 
    /// Address for both is: 111g3qjk7YzhdkKuMmTBzUB8gQ55YZ6MG
    /// </summary>
    public HashSet<string>? ScriptPubKeyHashes { get; } = null;

    public ScriptNode(
        string address,
        ScriptType scriptType,
        string? hexBase64 = null,
        double? originalIndegree = null,
        double? originalOutdegree = null,
        double? hopsFromRoot = null,
        string? idInGraphDb = null) :
        base(
            id: address,
            originalInDegree: originalIndegree,
            originalOutDegree: originalOutdegree,
            outHopsFromRoot: hopsFromRoot,
            idInGraphDb: idInGraphDb)
    {
        Address = address;
        ScriptType = scriptType;
        HexBase64 = hexBase64;
    }

    public ScriptNode(
        ScriptPubKey scriptPubKey,
        double? originalIndegree = null,
        double? originalOutdegree = null,
        double? hopsFromRoot = null,
        string? idInGraphDb = null) :
        base(id: scriptPubKey.Address,
             originalInDegree: originalIndegree,
             originalOutDegree: originalOutdegree,
             outHopsFromRoot: hopsFromRoot,
             idInGraphDb: idInGraphDb)
    {
        Address = scriptPubKey.Address;
        ScriptType = scriptPubKey.ScriptType;

        if (ScriptType == ScriptType.nonstandard || ScriptType == ScriptType.NullData)
        {
            HexBase64 = scriptPubKey.HexBase58;
        }
    }

    public override string GetIdPropertyName()
    {
        return nameof(Address);
    }

    public static new string[] GetFeaturesName()
    {
        return [nameof(Address), nameof(ScriptType), .. Node.GetFeaturesName()];
    }

    public override string[] GetFeatures()
    {
        return [Address, ((double)ScriptType).ToString(), .. base.GetFeatures()];
    }

    public override bool HasNullFeatures()
    {
        return base.HasNullFeatures();
    }

    public override int GetHashCode()
    {
        // Do not add ID here, because ID is generated
        // in a multi-threaded process, hence cannot
        // guarantee a node's ID is reproducible.
        //
        // note that different locking scripts (e.g., P2PK and P2PKH)
        // paying to the same key-pair share the same Address,
        // hence the same node.
        // so, do not add any property here that can break that. 
        return Address.GetHashCode();
    }

    public int CompareTo(ScriptNode? other)
    {
        if (other == null) return -1;
        return string.CompareOrdinal(Address, other.Address);
    }

    public bool Equals(ScriptNode? other)
    {
        if (other == null)
            return false;

        return Address == other.Address;
    }

    public override string ToString()
    {
        return string.Join(
            Delimiter,
            new string[]
            {
                base.ToString(),
                ScriptType.ToString("d")
            });
    }
}
