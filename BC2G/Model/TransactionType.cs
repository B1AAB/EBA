namespace BC2G.Model
{
    /// <summary>
    /// Locking mechanisms or types of tranactions.
    /// 
    /// "The hashes used in P2PKH and P2SH outputs 
    /// are commonly encoded as Bitcoin addresses. 
    /// This is the procedure to encode those hashes 
    /// and decode the addresses."
    /// https://developer.bitcoin.org/reference/transactions.html
    /// 
    /// Read: https://developer.bitcoin.org/devguide/transactions.html
    /// </summary>
    public enum TransactionType
    {
        /// <summary>
        /// Pay-to-Public-Key-Hash:
        /// is the most common form of pubkey 
        /// script used to send a transaction to one 
        /// or multiple Bitcoin addresses.
        /// </summary>
        P2PKH,

        /// <summary>
        /// Pay-to-Script-Hash
        /// </summary>
        P2SH,
        P2SHH,
        P2WPKH,
        P2PK,
        Other
    }
}
