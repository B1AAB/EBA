namespace BC2G.DTO
{
    public class TxCacheItem
    {
        public string TxId { get; }
        public int VOut { get; }
        public string Address { get; }
        public double Value { get; }

        private const string _delimiter = "\t";

        public TxCacheItem(
            string address,
            double value,
            string txid = "",
            int vout = -1)
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

        public static TxCacheItem Deserialize(string value)
        {
            var cols = value.Split(_delimiter);
            return new TxCacheItem(
                txid: cols[0],
                vout: int.Parse(cols[1]),
                address: cols[2],
                value: double.Parse(cols[3]));
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
