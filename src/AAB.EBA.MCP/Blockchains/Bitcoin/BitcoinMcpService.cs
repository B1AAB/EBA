using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.Graph.Bitcoin.Descriptors;
using AAB.EBA.Graph.Db;
using AAB.EBA.Graph.Model;
using AAB.EBA.GraphDb;
using Neo4j.Driver;

namespace AAB.EBA.MCP.Blockchains.Bitcoin;

public class BitcoinMcpService(IGraphDb db, IGraphDb<BitcoinGraph>? bitcoinGraphDb = null)
{
    private readonly IGraphDb _db = db;
    private readonly IGraphDb<BitcoinGraph> _bitcoinGraphDb = bitcoinGraphDb;

    private static readonly BlockNodeDescriptor _blockNodeDescriptor = new();
    private static readonly Property _blockNodeHeightProp = _blockNodeDescriptor.Mapper.GetMapping(x => x.BlockMetadata.Height).Property;
    private static readonly ElementMapper<BlockNode> _blockNodeMapper = BlockNodeDescriptor.StaticMapper;

    private static readonly ScriptNodeDescriptor _scriptNodeDescriptor = new();
    private static readonly Property _addressProp = _scriptNodeDescriptor.Mapper.GetMapping(x => x.Address).Property;
    private static readonly Property _sha256Prop = _scriptNodeDescriptor.Mapper.GetMapping(x => x.SHA256Hash).Property;

    private static readonly TxNodeDescriptor _txNodeDescriptor = new();
    private static readonly Property _txidProp = _txNodeDescriptor.Mapper.GetMapping(x => x.Txid).Property;    

    private static readonly ElementMapper<T2SEdge> _t2sMapper = T2SEdgeDescriptor.StaticMapper;
    private static readonly ElementMapper<S2TEdge> _s2tMapper = S2TEdgeDescriptor.StaticMapper;
    private static readonly ElementMapper<B2TEdge> _b2tMapper = B2TEdgeDescriptor.StaticMapper;
    private static readonly ElementMapper<T2TEdge> _t2tMapper = T2TEdgeDescriptor.StaticMapper;

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
        var node = await _db.GetNodeAsync(NodeKind.Block, _blockNodeHeightProp.Name, height, ct);

        if (node == null)
            return null;

        return BlockNodeDescriptor.Deserialize(node.Properties, null, null, null, null);
    }

    public async Task<long?> GetLatestBlockHeightAsync(CancellationToken ct = default)
    {
        var nodes = await _db.FindNodesAsync(
            NodeKind.Block,
            ct,
            orderByProperty: _blockNodeHeightProp.Name,
            descending: true,
            limit: 1);

        if (nodes == null)
            return null;

        var node = nodes[0];

        if (node == null)
            return null;

        return _blockNodeMapper.GetValue(x => x.BlockMetadata.Height, node.Properties);
    }

    public async Task<TxDTO?> GetTxSummaryByTxidAsync(
        string txid,
        CancellationToken ct = default)
    {
        var edges = await _db.GetEdgesAsync(NodeKind.Tx, _txidProp.Name, txid, ct);

        if (edges == null)
            return null;

        long inValue = 0;
        long inValueGenerated = 0;
        long outValue = 0;
        long height = 0;
        long fee = 0;

        int totalInputScripts = 0;
        int totalOutputScripts = 0;
        var uniqueInputScripts = new HashSet<string>();
        var uniqueOutputScripts = new HashSet<string>();

        long minInputAge = long.MaxValue;
        long maxOutputAge = long.MinValue;
        long maxOutputSpentHeight = long.MinValue;
        long minOutputSpentHeight = long.MaxValue;

        decimal outputValueSpent = 0;

        foreach (var e in edges)
        {
            if (e.Type == T2SEdge.Kind.Relation.ToString())
            {
                var v = _t2sMapper.GetValue(x => x.Value, e.Properties);
                outValue += v;

                totalOutputScripts++;
                uniqueOutputScripts.Add(e.EndNodeElementId);

                var spentHeight = _t2sMapper.GetValue(x => x.SpentHeight, e.Properties);
                if (spentHeight != long.MaxValue) // created utxo is spent
                {
                    var creationHeight = _t2sMapper.GetValue(x => x.CreationHeight, e.Properties);
                    maxOutputSpentHeight = Math.Max(maxOutputSpentHeight, creationHeight);
                    minOutputSpentHeight = Math.Min(minOutputSpentHeight, creationHeight);

                    outputValueSpent += v;
                }
            }
            else if (e.Type == S2TEdge.Kind.Relation.ToString())
            {
                var value = _s2tMapper.GetValue(x => x.Value, e.Properties);
                inValue += value;

                if (_s2tMapper.GetValue(x => x.Generated, e.Properties) == true)
                    inValueGenerated += value;

                totalInputScripts++;
                uniqueInputScripts.Add(e.StartNodeElementId);

                var age =
                    _s2tMapper.GetValue(x => x.SpentHeight, e.Properties) -
                    _s2tMapper.GetValue(x => x.CreationHeight, e.Properties);

                minInputAge = Math.Min(minInputAge, age);
                maxOutputAge = Math.Max(maxOutputAge, age);
            }
            else if (e.Type == B2TEdge.Kind.Relation.ToString())
            {
                height = _b2tMapper.GetValue(x => x.Height, e.Properties);
            }
            else if (e.Type == T2TEdge.KindFee.ToString())
            {
                fee = _t2tMapper.GetValue(x => x.Value, e.Properties);
            }
        }

        return new TxDTO(
            Height: height,
            Fee: fee,
            InValue: inValue,
            OutValue: outValue,
            InValueGenerated: inValueGenerated,
            TotalInputScripts: totalInputScripts,
            TotalOutputScripts: totalOutputScripts,
            UniqueInputScripts: uniqueInputScripts.Count,
            UniqueOutputScripts: uniqueOutputScripts.Count,
            MinInputAge: minInputAge,
            MaxOutputAge: maxOutputAge,
            MaxOutputSpentHeight: maxOutputSpentHeight,
            MinOutputSpentHeight: minOutputSpentHeight,
            OutputValueSpent: outputValueSpent);
    }

    public async Task<BitcoinGraph> GetTxNodeNeighborsAsync(
        string txid,
        CancellationToken ct = default)
    {
        return await GetNodeNeighborsAsync(NodeKind.Tx, _txidProp.Name, txid, ct: ct);
    }

    public async Task<BitcoinGraph> GetScriptNodeNeighbors(
        string? sha = null,
        string? address = null,
        CancellationToken ct = default)
    {
        if (sha == null && address == null)
            throw new ArgumentException("Either sha or address must be provided.");

        if (sha != null)
            return await GetNodeNeighborsAsync(NodeKind.Script, _sha256Prop.Name, sha, ct: ct);
        else
            return await GetNodeNeighborsAsync(NodeKind.Script, _addressProp.Name, address!, ct: ct);
    }

    public async Task<BitcoinGraph> GetNodeNeighborsAsync(
        NodeKind nodeKind,
        string idPropertyName,
        string idValue,
        int queryLimit = 100,
        int maxLevel = 1,
        bool useBFS = true,
        CancellationToken ct = default)
    {
        var neighbors = await _db.GetNeighborsAsync(
            rootNodeLabel: nodeKind,
            rootNodeIdProperty: idPropertyName,
            rootNodeId: idValue,
            queryLimit: queryLimit,
            maxLevel: maxLevel,
            useBFS: useBFS,
            ct: ct);

        return NeighborsToGraph(neighbors);
    }

    private BitcoinGraph NeighborsToGraph(List<IRecord> neighbors)
    {
        var g = new BitcoinGraph();
        if (neighbors.Count == 0)
            return g;

        var hop = 1;

        var edges = new List<IRelationship>();
        var rootList = neighbors[0]["root"].As<List<object>>();
        if (!TryUnpackNodeDict(
            rootList[0].As<IDictionary<string, object>>(), 
            hop, out var builtRootNode) || builtRootNode == null)
            return g;

        var rootNode = g.GetOrAddNode(builtRootNode);

        g.AddLabel("RootNodeId", rootNode.Id);
        var nodeDbidToIdMap = new Dictionary<string, string>();

        for (int i = 1; i < neighbors.Count; i++)
        {
            var r = neighbors[i];
            foreach (var nodeObject in r["nodes"].As<List<object>>())
            {
                if (!TryUnpackNodeDict(nodeObject.As<IDictionary<string, object>>(), hop, out var node)
                    || node == null
                    || node.IdInGraphDb == null)
                    continue;

                g.GetOrAddNode(node);
                nodeDbidToIdMap[node.IdInGraphDb] = node.Id;
            }

            foreach (var edge in r.Values["relationships"].As<List<IRelationship>>())
                edges.Add(edge);
        }

        foreach (var edge in edges)
        {
            string subjectNodeGraphDbId;
            if (edge.StartNodeElementId == rootNode.IdInGraphDb)
                subjectNodeGraphDbId = edge.EndNodeElementId;
            else if (edge.EndNodeElementId == rootNode.IdInGraphDb)
                subjectNodeGraphDbId = edge.StartNodeElementId;
            else
                continue; // edge is not connected to rootNode

            g.TryGetNode(nodeDbidToIdMap[subjectNodeGraphDbId], out var subjectNode);
            if (subjectNode == null)
                continue;

            IEdge<Graph.Model.INode, Graph.Model.INode> candidateEdge =
                edge.StartNodeElementId == rootNode.IdInGraphDb ?
                _bitcoinGraphDb.StrategyFactory.CreateEdge(rootNode, subjectNode, edge) :
                _bitcoinGraphDb.StrategyFactory.CreateEdge(subjectNode, rootNode, edge);
            g.TryGetOrAddEdge(candidateEdge, out candidateEdge);
        }

        return g;
    }


    private bool TryUnpackNodeDict(IDictionary<string, object> dict, double hop, out Graph.Model.INode? v)
    {
        v = null;
        var node = dict["node"].As<Neo4j.Driver.INode>();
        var inDegree = Convert.ToDouble(dict["inDegree"]);
        var outDegree = Convert.ToDouble(dict["outDegree"]);
        if (node is null)
            return false;
        return
            _bitcoinGraphDb.StrategyFactory.TryCreateNode(
                node: node,
                out v,
                originalIndegree: inDegree,
                originalOutdegree: outDegree,
                outHopsFromRoot: hop);
    }
}
