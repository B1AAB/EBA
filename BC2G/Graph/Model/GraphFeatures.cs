namespace BC2G.Graph.Model;

public class GraphFeatures
{
    // TODO: make sure all the public types in the following are immutable.

    public Dictionary<GraphComponentType, List<double[]>> NodeFeatures { get; }
    public Dictionary<GraphComponentType, string[]> NodeFeaturesHeader { get; }

    public Dictionary<GraphComponentType, List<double[]>> EdgeFeatures { get; }
    public Dictionary<GraphComponentType, string[]> EdgeFeaturesHeader { get; }

    public ReadOnlyCollection<double[]> EdgeFeaturesOld { get; }
    public ReadOnlyCollection<string> EdgeFeaturesHeaderOld { get; }

    public ReadOnlyCollection<int[]> PairIndices { get; }
    public ReadOnlyCollection<string> PairIndicesHeader { get; }

    public ReadOnlyCollection<string> Labels { get; }
    public ReadOnlyCollection<string> LabelsHeader { get; }

    public GraphFeatures(GraphBase graph)
    {
        LabelsHeader = new ReadOnlyCollection<string>(["GraphID", "ConnectedGraph_or_Forest"]);

        NodeFeaturesHeader = [];
        NodeFeaturesHeader.Add(GraphComponentType.BitcoinBlockNode, BlockNode.GetFeaturesName());
        NodeFeaturesHeader.Add(GraphComponentType.BitcoinTxNode, TxNode.GetFeaturesName());
        NodeFeaturesHeader.Add(GraphComponentType.BitcoinScriptNode, ScriptNode.GetFeaturesName());
        NodeFeaturesHeader.Add(GraphComponentType.BitcoinCoinbaseNode, CoinbaseNode.GetFeaturesName());

        // TODO: extend to support all types of edges in the graph db. 
        EdgeFeaturesHeader = [];
        EdgeFeaturesHeader.Add(GraphComponentType.BitcoinC2T, C2TEdge.GetFeaturesName());
        EdgeFeaturesHeader.Add(GraphComponentType.BitcoinC2S, C2SEdge.GetFeaturesName());
        EdgeFeaturesHeader.Add(GraphComponentType.BitcoinT2T, T2TEdge.GetFeaturesName());
        EdgeFeaturesHeader.Add(GraphComponentType.BitcoinS2S, S2SEdge.GetFeaturesName());

        var nodeFeatures = new Dictionary<GraphComponentType, List<double[]>>();
        var nodeIdToIdx = new Dictionary<GraphComponentType, Dictionary<string, int>>();
        var nodeGraphComponentTypes = new[]
        {
            GraphComponentType.BitcoinBlockNode,
            GraphComponentType.BitcoinTxNode,
            GraphComponentType.BitcoinScriptNode,
            GraphComponentType.BitcoinCoinbaseNode
        };

        foreach (var nodeType in nodeGraphComponentTypes)
        {
            nodeFeatures.Add(nodeType, []);
            nodeIdToIdx.Add(nodeType, []);
        }

        var edgeFeatures = new Dictionary<GraphComponentType, List<double[]>>();
        var edgeGraphComponentTypes = new[]
        {
            GraphComponentType.BitcoinC2T,
            GraphComponentType.BitcoinC2S,
            GraphComponentType.BitcoinT2T,
            GraphComponentType.BitcoinS2S
        };
        foreach (var edgeType in edgeGraphComponentTypes)
        {
            edgeFeatures.Add(edgeType, []);
        }

        foreach (var node in graph.Nodes)
        {
            var gComponentType = node.GetGraphComponentType();
            nodeFeatures[gComponentType].Add(node.GetFeatures());
            nodeIdToIdx[gComponentType].Add(node.Id, nodeIdToIdx[gComponentType].Count);
        }

        foreach (var edge in graph.Edges)
        {
            // TODO: this is a hack to make sure that the source node of a C2T or C2S edge is a coinbase node,
            // it is set incorrectly by default due to a design issue with C2T and C2S edges.
            // First you need to fix those edges, then remove this.
            var edgeGraphComponentType = edge.GetGraphComponentType();
            var sourceNodeIdx = 0.0; // so the index of the coinbase is 0 because only one node will be in that file
            if (edgeGraphComponentType != GraphComponentType.BitcoinC2T && edgeGraphComponentType != GraphComponentType.BitcoinC2S)
                sourceNodeIdx = nodeIdToIdx[edge.Source.GetGraphComponentType()][edge.Source.Id];

            edgeFeatures[edgeGraphComponentType].Add(
            [
                .. (new double[] {
                    sourceNodeIdx,
                    nodeIdToIdx[edge.Target.GetGraphComponentType()][edge.Target.Id] }),
                .. edge.GetFeatures(),
            ]);
        }

        NodeFeatures = nodeFeatures;
        EdgeFeatures = edgeFeatures;

        Labels = new ReadOnlyCollection<string>(
            [graph.Id, .. graph.Labels.Select(t => t.ToString())]);
    }
}
