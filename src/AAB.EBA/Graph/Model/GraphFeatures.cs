namespace AAB.EBA.Graph.Model;

public class GraphFeatures
{
    public Dictionary<NodeKind, List<string[]>> NodeFeatures { get; }
    public Dictionary<NodeKind, string[]> NodeFeaturesHeader { get; }

    public Dictionary<EdgeKind, List<double[]>> EdgeFeatures { get; }
    public Dictionary<EdgeKind, string[]> EdgeFeaturesHeader { get; }

    public ReadOnlyCollection<double[]> EdgeFeaturesOld { get; }
    public ReadOnlyCollection<string> EdgeFeaturesHeaderOld { get; }

    public ReadOnlyCollection<int[]> PairIndices { get; }
    public ReadOnlyCollection<string> PairIndicesHeader { get; }

    public ReadOnlyCollection<string> Labels { get; }
    public ReadOnlyCollection<string> LabelsHeader { get; }

    public GraphFeatures(GraphBase graph)
    {
        // TODO: add a check to this method to make sure no NaN feature is returned.
        // or maybe in the node or graph methods to ensure none of the feature get NaN or null value.

        LabelsHeader = new ReadOnlyCollection<string>(["GraphID", "RootNodeId", "RootNodeIdx", "NodeCount", "EdgeCount"]);

        NodeFeaturesHeader = [];
        NodeFeaturesHeader.Add(BlockNode.Kind, ["Index", .. BlockNode.GetFeaturesName()]);
        NodeFeaturesHeader.Add(TxNode.Kind, ["Index", .. TxNode.GetFeaturesName()]);
        NodeFeaturesHeader.Add(ScriptNode.Kind, ["Index", .. ScriptNode.GetFeaturesName()]);
        NodeFeaturesHeader.Add(CoinbaseNode.Kind, ["Index", .. CoinbaseNode.GetFeaturesName()]);

        var sourceAndTarget = new[] { "Source", "Target" };
        EdgeFeaturesHeader = [];
        EdgeFeaturesHeader.Add(C2TEdge.Kind, [.. sourceAndTarget, .. C2TEdge.GetFeaturesName()]);
        EdgeFeaturesHeader.Add(B2TEdge.Kind, [.. sourceAndTarget, .. B2TEdge.GetFeaturesName()]);
        EdgeFeaturesHeader.Add(S2TEdge.Kind, [.. sourceAndTarget, .. S2TEdge.GetFeaturesName()]);
        EdgeFeaturesHeader.Add(T2SEdge.Kind, [.. sourceAndTarget, .. T2SEdge.GetFeaturesName()]);
        EdgeFeaturesHeader.Add(T2TEdge.KindFee, [.. sourceAndTarget, .. T2TEdge.GetFeaturesName()]);
        EdgeFeaturesHeader.Add(T2TEdge.KindTransfers, [.. sourceAndTarget, .. T2TEdge.GetFeaturesName()]);
        EdgeFeaturesHeader.Add(B2BEdge.Kind, [.. sourceAndTarget, .. B2BEdge.GetFeaturesName()]);

        var nodeFeatures = new Dictionary<NodeKind, List<string[]>>();
        var nodeIdToIdx = new Dictionary<NodeKind, Dictionary<string, int>>();

        var nodeGraphComponentTypes = NodeFeaturesHeader.Keys.ToArray();
        foreach (var nodeType in nodeGraphComponentTypes)
        {
            nodeFeatures.Add(nodeType, []);
            nodeIdToIdx.Add(nodeType, []);
        }

        var edgeFeatures = new Dictionary<EdgeKind, List<double[]>>();
        var edgeGraphComponentTypes = EdgeFeaturesHeader.Keys.ToArray();
        foreach (var edgeKind in edgeGraphComponentTypes)
        {
            edgeFeatures.Add(edgeKind, []);
        }

        foreach (var nodeType in graph.NodesByType)
        {
            foreach (var node in nodeType.Value)
            {
                var nodeIndex = nodeIdToIdx[nodeType.Key].Count;
                nodeIdToIdx[nodeType.Key].Add(node.Id, nodeIndex);
                nodeFeatures[nodeType.Key].Add([nodeIndex.ToString(), .. node.GetFeatures()]);
            }
        }

        /*
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
        }*/

        foreach (var edgeKind in graph.EdgesByType)
        {
            foreach (var edge in edgeKind.Value)
            {
                edgeFeatures[edgeKind.Key].Add(
                [
                    nodeIdToIdx[edge.Source.NodeKind][edge.Source.Id],
                    nodeIdToIdx[edge.Target.NodeKind][edge.Target.Id],
                    .. edge.GetFeatures(),
                ]);
            }
        }

        NodeFeatures = nodeFeatures;
        EdgeFeatures = edgeFeatures;

        /*
        Labels = new ReadOnlyCollection<string>(
            [graph.Id, .. graph.Labels.Select(t => t.ToString())]);*/
        // TODO: the following is a hack; root node type should not be hardcored.
        var gLabels = graph.Labels;
        Labels = new ReadOnlyCollection<string>(
            [
                graph.Id, 
                //gLabels["ConnectedGraph_or_Forest"], 
                gLabels["RootNodeId"],
                nodeIdToIdx[ScriptNode.Kind][gLabels["RootNodeId"]].ToString(),
                graph.Nodes.Count.ToString(),
                graph.Edges.Count.ToString()
            ]);
    }
}
