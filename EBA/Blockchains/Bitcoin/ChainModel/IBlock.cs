using EBA.Utilities;

namespace EBA.Blockchains.Bitcoin.ChainModel;

public interface IBlock
{
    public string Hash { get; }

    public int Confirmations { get; }

    public long Height { get; }

    public ulong Version { get; }

    public string VersionHex { get; }

    public string Merkleroot { get; }

    public uint Time { get; }

    /// <summary>
    /// See the following BIP on mediantime diff. compared to time.
    /// https://github.com/bitcoin/bips/blob/master/bip-0113.mediawiki
    /// </summary>
    public uint MedianTime { get; }

    public ulong Nonce { get; }

    public string Bits { get; }

    public double Difficulty { get; }

    public string Chainwork { get; }

    public int TransactionsCount { get; }

    public string PreviousBlockHash { get; }

    public string NextBlockHash { get; }

    public int StrippedSize { get; }

    public int Size { get; }

    public int Weight { get; }

    public int CoinbaseOutputsCount { get; }

    public long TxFees { get; }

    public long MintedBitcoins { get; }

    public DescriptiveStatistics InputCounts { get; }
    public DescriptiveStatistics OutputCounts { get; }

    public DescriptiveStatistics InputValues { get; }
    public DescriptiveStatistics OutputValues { get; }

    public DescriptiveStatistics SpentOutputAge { get; }
}
