namespace BC2G.Model
{
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
        multisig,

        /// <summary>
        /// A script with this is often used to 
        /// encode data in the blockchain, where 
        /// a small amount of arbitrary data is 
        /// added to the block chain in exchange 
        /// for paying a transaction fee.
        /// <seealso cref="https://developer.bitcoin.org/devguide/transactions.html#null-data"/>
        /// </summary>
        NullData,

        Unknown
    }
}
