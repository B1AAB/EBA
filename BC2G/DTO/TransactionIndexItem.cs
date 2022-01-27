namespace BC2G.DTO
{
    public class TransactionIndexItem
    {
        public string TxId { get; }
        public int VOut { get; }
        public string Address { get; }
        public double Value { get; }

        private const string _delimiter = "\t";

        public TransactionIndexItem(
            string txid,
            int vout,
            string address,
            double value)
        {
            TxId = txid;
            VOut = vout;
            Address = address;
            Value = value;
        }

        public static string GetHeader()
        {
            return string.Join(
                _delimiter,
                new string[] { "txid", "vout", "address", "value" });
        }

        public override string ToString()
        {
            return string.Join(
                _delimiter,
                new string[] {
                    TxId,
                    VOut.ToString(),
                    Address,
                    Value.ToString(),
                    Environment.NewLine });
        }
    }
}
