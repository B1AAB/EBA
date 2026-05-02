using AAB.EBA.GraphDb;
using EBA.Blockchains.Bitcoin.GraphModel;
using EBA.Graph.Bitcoin.Descriptors;
using EBA.Graph.Model;

namespace AAB.EBA.MCP.Blockchains.Bitcoin;

public class BitcoinMcpService(IGraphDb db)
{
    private readonly IGraphDb _db = db;

    private static readonly BlockNodeDescriptor _blockNodeDescriptor = new();
    private static readonly Property _blockNodeHeightProp = _blockNodeDescriptor.Mapper.GetMapping(x => x.BlockMetadata.Height).Property;

    private static readonly ScriptNodeDescriptor _scriptNodeDescriptor = new();
    private static readonly Property _addressProp = _scriptNodeDescriptor.Mapper.GetMapping(x => x.Address).Property;
    private static readonly Property _sha256Prop = _scriptNodeDescriptor.Mapper.GetMapping(x => x.SHA256Hash).Property;

    private static readonly ElementMapper<T2SEdge> _t2sMapper = T2SEdgeDescriptor.StaticMapper;

    public async Task<ScriptNode?> GetScriptByAddressAsync(
        string address, 
        CancellationToken ct = default)
    {
        var node = await _db.GetNodeAsync(NodeKind.Script, _addressProp.Name, address, ct);

        if (node == null)
            return null;

        return ScriptNodeDescriptor.Deserialize(node.Properties, null, null, null, null);
    }

    public async Task<ScriptNode?> GetScriptBySHA256Async(
        string sha, 
        CancellationToken ct = default)
    {
        var node = await _db.GetNodeAsync(NodeKind.Script, _sha256Prop.Name, sha, ct);

        if (node == null)
            return null;

        return ScriptNodeDescriptor.Deserialize(node.Properties, null, null, null, null);
    }

    public async Task<long?> GetScriptBalanceAsync(
        string sha, 
        long height = long.MaxValue, 
        CancellationToken ct = default)
    {
        var edges = await _db.GetEdgesAsync(NodeKind.Script, _sha256Prop.Name, sha, ct);

        if (edges == null || edges.Count == 0)
            return null;

        var sortedEdges = edges.SortByRelevantHeight();
        long balance = 0;
        foreach (var edge in sortedEdges)
        {
            var creationH = _t2sMapper.GetValue(x => x.CreationHeight, edge.Properties);
            var spentH = _t2sMapper.GetValue(x => x.SpentHeight, edge.Properties);

            if (creationH > height)
                break;

            if (edge.Type == T2SEdge.Kind.Relation.ToString() && spentH == long.MaxValue)
            {
                balance += _t2sMapper.GetValue(x => x.Value, edge.Properties);
            }
        }

        return balance;
    }

    public async Task<ScriptTxSummaryStats?> GetScriptTxSummaryStatsAsync(
        string sha, 
        CancellationToken ct = default)
    {
        var edges = await _db.GetEdgesAsync(NodeKind.Script, _sha256Prop.Name, sha, ct);

        if (edges == null || edges.Count == 0)
            return null;

        var sortedEdges = edges.SortByRelevantHeight();
        long totalReceived = 0;
        long totalSent = 0;
        bool firstReceivedSet = false;
        long firstReceivedValue = 0;
        long firstReceivedHeight = long.MaxValue;
        long lastReceivedValue = long.MaxValue;
        long lastReceivedHeight = long.MaxValue;

        bool firstSentSet = false;
        long firstSentValue = 0;
        long firstSentHeight = long.MaxValue;
        long lastSentValue = long.MaxValue;
        long lastSentHeight = long.MaxValue;

        long v = 0;
        long h = 0;
        foreach (var edge in sortedEdges)
        {
            if (edge.Type == T2SEdge.Kind.Relation.ToString())
            {
                v = _t2sMapper.GetValue(x => x.Value, edge.Properties);
                h = _t2sMapper.GetValue(x => x.CreationHeight, edge.Properties);

                totalReceived += v;

                if (!firstReceivedSet)
                {
                    firstReceivedValue = v;
                    firstReceivedHeight = h;
                    firstReceivedSet = true;
                }

                lastReceivedValue = v;
                lastReceivedHeight = h;
            }
            else if (edge.Type == S2TEdge.Kind.Relation.ToString())
            {
                v = _t2sMapper.GetValue(x => x.Value, edge.Properties);
                h = _t2sMapper.GetValue(x => x.CreationHeight, edge.Properties);

                totalSent += v;

                if (!firstSentSet)
                {
                    firstSentValue = v;
                    firstSentHeight = h;
                    firstSentSet = true;
                }

                lastSentValue = v;
                lastSentHeight = h;
            }
        }

        return new ScriptTxSummaryStats(
            TxCount: edges.Count,
            TotalReceived: totalReceived,
            TotalSent: totalSent,
            FirstReceivedHeight: firstReceivedHeight,
            FirstReceivedValue: firstReceivedValue,
            FirstSentHeight: firstSentHeight,
            FirstSentValue: firstSentValue,
            LastReceivedHeight: lastReceivedHeight,
            LastReceivedValue: lastReceivedValue,
            LastSentHeight: lastSentHeight,
            LastSentValue: lastSentValue
        );
    }

    public async Task<BlockNode?> GetBlockByHeightAsync(
        long height, 
        CancellationToken ct = default)
    {
        var node = await _db.GetNodeAsync(NodeKind.Block, _blockNodeHeightProp.Name, height.ToString(), ct);

        if (node == null)
            return null;

        return BlockNodeDescriptor.Deserialize(node.Properties, null, null, null, null);
    }
}
