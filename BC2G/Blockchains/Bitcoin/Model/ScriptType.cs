namespace BC2G.Blockchains.Bitcoin.Model;

// DO NOT RENAME THESE, EXCEPT FOR UNKNOWN, SINCE 
// ALL THE KEYS CORRESPOND TO THE `type` ATTRIBUTE
// IN BITCOIN TRANSACTIONS.
// ENUM TYPES ARE CASE-INSENSITIVE.

public enum ScriptType
{
    PubKey,

    PubKeyHash,

    /// <summary>
    /// Pay-to-script-hash. 
    /// <see cref="https://bitcoin.stackexchange.com/a/9703/129532"/>
    /// </summary>
    ScriptHash,

    /// <summary>
    /// Native Segwit version of a pay-to-public-key-hash.
    /// </summary>
    witness_v0_keyhash,
    
    witness_v0_scripthash,
    witness_v1_taproot,

    /// <summary>
    /// Example: Block: 710061, Txid: c24bd72e3eaea797bd5c879480a0db90980297bc7085efda97df2bf7d31413fb, vout: 0, address: bc1pm9jzmujvdqjj6y28hptk859zs3yyv78hlz84pm
    /// </summary>
    witness_unknown,

    multisig,
    nonstandard, // e.g., block #71036

    /// <summary>
    /// A script with this is often used to 
    /// encode data in the blockchain, where 
    /// a small amount of arbitrary data is 
    /// added to the block chain in exchange 
    /// for paying a transaction fee.
    /// <seealso cref="https://developer.bitcoin.org/devguide/transactions.html#null-data"/>
    /// </summary>
    NullData,

    Coinbase,
    Unknown
}
