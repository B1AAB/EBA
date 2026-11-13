namespace AAB.EBA.Tests.TestKit.BitcoinCore;

/// <summary>
/// The REST endpoints of Bitcoin Core that a Bitcoin traverse uses.
/// </summary>
public enum BitcoinCoreEndpoint
{
    ChainInfo,
    BlockHashByHeight,
    Block
}

/// <summary>
/// A request received by a <see cref="SyntheticBitcoinNode"/>. <see cref="Height"/>
/// is the height of the requested block (for the block endpoint, derived from the
/// requested hash), or null for the endpoints that are not about a block.
/// </summary>
public sealed record BitcoinCoreRequest(BitcoinCoreEndpoint Endpoint, long? Height);
