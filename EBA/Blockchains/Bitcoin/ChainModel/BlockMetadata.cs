using EBA.Utilities;

namespace EBA.Blockchains.Bitcoin.ChainModel;

public class BlockMetadata
{
    [JsonPropertyName("hash")]
    public string Hash { init; get; } = string.Empty;

    [JsonPropertyName("confirmations")]
    public int Confirmations { init; get; }

    [JsonPropertyName("height")]
    public long Height { init; get; }

    [JsonPropertyName("version")]
    public ulong Version { init; get; }

    [JsonPropertyName("versionHex")]
    public string VersionHex { init; get; } = string.Empty;

    [JsonPropertyName("merkleroot")]
    public string Merkleroot { init; get; } = string.Empty;

    [JsonPropertyName("time")]
    public uint Time { init; get; }

    /// <summary>
    /// See the following BIP on mediantime diff. compared to time.
    /// https://github.com/bitcoin/bips/blob/master/bip-0113.mediawiki
    /// </summary>
    [JsonPropertyName("mediantime")]
    public uint MedianTime { init; get; }

    [JsonPropertyName("nonce")]
    public ulong Nonce { init; get; }

    [JsonPropertyName("bits")]
    public string Bits { init; get; } = string.Empty;

    [JsonPropertyName("difficulty")]
    public double Difficulty { init; get; }

    [JsonPropertyName("chainwork")]
    public string Chainwork { init; get; } = string.Empty;

    [JsonPropertyName("nTx")]
    public int TransactionsCount { init; get; }

    [JsonPropertyName("previousblockhash")]
    public string PreviousBlockHash { init; get; } = string.Empty;

    [JsonPropertyName("nextblockhash")]
    public string NextBlockHash { init; get; } = string.Empty;

    [JsonPropertyName("strippedsize")]
    public int StrippedSize { init; get; }

    [JsonPropertyName("size")]
    public int Size { init; get; }

    [JsonPropertyName("weight")]
    public int Weight { init; get; }

    public virtual int CoinbaseOutputsCount { init; get; }
    public virtual long TxFees { init; get; }
    public virtual long MintedBitcoins { init; get; }

    public virtual DescriptiveStatistics? InputCounts { init; get; }
    public virtual DescriptiveStatistics? OutputCounts { init; get; }
    public virtual DescriptiveStatistics? InputValues { init; get; }
    public virtual DescriptiveStatistics? OutputValues { init; get; }
    public virtual DescriptiveStatistics? SpentOutputAge { init; get; }

    public virtual Dictionary<ScriptType, uint> ScriptTypeCount { init; get; } = [];
}
