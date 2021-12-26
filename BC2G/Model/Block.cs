using System.Text.Json.Serialization;

namespace BC2G.Model
{
    public class Block
    {
        [JsonPropertyName("hash")]
        public string Hash { set; get; } = string.Empty;

        [JsonPropertyName("confirmations")]
        public int Confirmations { set; get; }

        [JsonPropertyName("height")]
        public int Height { set; get; }

        [JsonPropertyName("version")]
        public int Version { set; get; }

        [JsonPropertyName("versionHex")]
        public string VersionHex { set; get; } = string.Empty;

        [JsonPropertyName("merkleroot")]
        public string Merkleroot { set; get; } = string.Empty;

        [JsonPropertyName("time")]
        public uint Time { set; get; }

        [JsonPropertyName("mediantime")]
        public uint MedianTime { set; get; }

        [JsonPropertyName("nonce")]
        public ulong Nonce { set; get; }

        [JsonPropertyName("bits")]
        public string Bits { set; get; } = string.Empty;

        [JsonPropertyName("difficulty")]
        public double Difficulty { set; get; }

        [JsonPropertyName("chainwork")]
        public string Chainwork { set; get; } = string.Empty;

        [JsonPropertyName("nTx")]
        public int TransactionsCount { set; get; }

        [JsonPropertyName("previousblockhash")]
        public string PreviousBlockHash { set; get; } = string.Empty;

        [JsonPropertyName("nextblockhash")]
        public string NextBlockHash { set; get; } = string.Empty;

        [JsonPropertyName("strippedsize")]
        public int StrippedSize { set; get; }

        [JsonPropertyName("size")]
        public int Size { set; get; }

        [JsonPropertyName("weight")]
        public int Weight { set; get; }

        [JsonPropertyName("tx")]
        public List<Transaction> Transactions { set; get; } = new List<Transaction>();
    }
}
