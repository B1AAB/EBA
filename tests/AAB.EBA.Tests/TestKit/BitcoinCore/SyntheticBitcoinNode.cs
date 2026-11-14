using System;
using System.Globalization;
using System.Text.Json.Nodes;

namespace AAB.EBA.Tests.TestKit.BitcoinCore;

/// <summary>
/// An in-memory stand-in for the REST interface of a Bitcoin Core node,
/// to be used as the primary handler of the <see cref="HttpClient"/> that
/// <see cref="BitcoinChainAgent"/> uses.
///
/// It serves a chain of any length by re-issuing a single template block
/// (taken from the mock data) at every requested height. Each served block
/// has its own height and a hash derived from that height, so every block
/// is a distinct block for the traverse, while their contents (transactions,
/// scripts, etc.) are identical. This makes it possible to traverse many
/// blocks without having mock data for each of them.
///
/// Every request is logged (see <see cref="Requests"/>), and how requests are
/// answered can be changed per endpoint and height with <see cref="On"/>
/// (e.g., to fail, stall, or delay a request), which lets tests drive a
/// client to a known point.
/// </summary>
public sealed class SyntheticBitcoinNode : BitcoinNodeStub
{
    public const long DefaultTemplateBlockHeight = 71036;

    private readonly JsonObject _templateBlock;

    public SyntheticBitcoinNode(string chainInfo, string templateBlock)
        : base(chainInfo)
    {
        _templateBlock = JsonNode.Parse(templateBlock)?.AsObject()
            ?? throw new ArgumentException("Invalid template block.", nameof(templateBlock));
    }

    /// <summary>
    /// Creates a node that uses the mock chain info and the mock
    /// block at <paramref name="templateBlockHeight"/> as the template.
    /// </summary>
    public static SyntheticBitcoinNode FromMockData(
        long templateBlockHeight = DefaultTemplateBlockHeight)
        => new(MockData.ChainInfo(), MockData.BlockJson(templateBlockHeight));

    /// <summary>
    /// The hash this node assigns to the block at the given height.
    /// </summary>
    public static string GetBlockHash(long height)
        => height.ToString("x64", CultureInfo.InvariantCulture);

    protected override string GetBlockHashCore(long height)
        => GetBlockHash(height);

    protected override long? ResolveHeight(string hash)
    {
        return long.TryParse(hash, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var height)
            ? height
            : null;
    }

    protected override string GetBlock(long height)
    {
        var block = _templateBlock.DeepClone().AsObject();
        block["height"] = height;
        block["hash"] = GetBlockHash(height);
        block["previousblockhash"] = GetBlockHash(height - 1);
        block["nextblockhash"] = GetBlockHash(height + 1);
        return block.ToJsonString();
    }
}
