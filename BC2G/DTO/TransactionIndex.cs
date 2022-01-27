namespace BC2G.DTO
{
    // This class is only used to store data, and 
    // the stored data is not currently used. The 
    // data is meant to act as a backup for utxo,
    // but if it turns out not necessary, it should
    // be safe to delete this type. 

    public class TransactionIndex : PersistentObject<TransactionIndexItem>
    {
        public TransactionIndex(
            string filename,
            CancellationToken cancellationToken) : base(
                filename,
                cancellationToken,
                TransactionIndexItem.GetHeader())
        { }

        public override string Serialize(
            TransactionIndexItem obj,
            CancellationToken cancellationToken)
        {
            return obj.ToString();
        }
    }
}
