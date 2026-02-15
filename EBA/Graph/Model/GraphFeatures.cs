namespace EBA.Graph.Model;

public class GraphFeatures
{
    public Dictionary<Type, List<string[]>> NodeFeatures { get; }
    public Dictionary<Type, string[]> NodeFeaturesHeader { get; }

    public Dictionary<Type, List<double[]>> EdgeFeatures { get; }
    public Dictionary<Type, string[]> EdgeFeaturesHeader { get; }

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
        NodeFeaturesHeader.Add(typeof(BlockNode), ["Index", .. BlockNode.GetFeaturesName()]);
        NodeFeaturesHeader.Add(typeof(TxNode), ["Index", .. TxNode.GetFeaturesName()]);
        NodeFeaturesHeader.Add(typeof(ScriptNode), ["Index", .. ScriptNode.GetFeaturesName()]);
        NodeFeaturesHeader.Add(typeof(CoinbaseNode), ["Index", .. CoinbaseNode.GetFeaturesName()]);

        var sourceAndTarget = new[] { "Source", "Target" };
        EdgeFeaturesHeader = [];
        EdgeFeaturesHeader.Add(typeof(C2TEdge), [.. sourceAndTarget, .. C2TEdge.GetFeaturesName()]);
        EdgeFeaturesHeader.Add(typeof(T2TEdge), [.. sourceAndTarget, .. T2TEdge.GetFeaturesName()]);
        EdgeFeaturesHeader.Add(typeof(T2BEdge), [.. sourceAndTarget, .. T2BEdge.GetFeaturesName()]);
        EdgeFeaturesHeader.Add(typeof(B2TEdge), [.. sourceAndTarget, .. B2TEdge.GetFeaturesName()]);
        EdgeFeaturesHeader.Add(typeof(S2TEdge), [.. sourceAndTarget, .. S2TEdge.GetFeaturesName()]);
        EdgeFeaturesHeader.Add(typeof(T2SEdge), [.. sourceAndTarget, .. T2SEdge.GetFeaturesName()]);

        var nodeFeatures = new Dictionary<Type, List<string[]>>();
        var nodeIdToIdx = new Dictionary<Type, Dictionary<string, int>>();

        var nodeGraphComponentTypes = NodeFeaturesHeader.Keys.ToArray();
        foreach (var nodeType in nodeGraphComponentTypes)
        {
            nodeFeatures.Add(nodeType, []);
            nodeIdToIdx.Add(nodeType, []);
        }

        var edgeFeatures = new Dictionary<Type, List<double[]>>();
        var edgeGraphComponentTypes = EdgeFeaturesHeader.Keys.ToArray();
        foreach (var edgeType in edgeGraphComponentTypes)
        {
            edgeFeatures.Add(edgeType, []);
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

        foreach (var edgeType in graph.EdgesByType)
        {
            foreach (var edge in edgeType.Value)
            {
                edgeFeatures[edgeType.Key].Add(
                [
                    nodeIdToIdx[edge.Source.GetType()][edge.Source.Id],
                    nodeIdToIdx[edge.Target.GetType()][edge.Target.Id],
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
                nodeIdToIdx[typeof(ScriptNode)][gLabels["RootNodeId"]].ToString(),
                graph.Nodes.Count.ToString(),
                graph.Edges.Count.ToString()
            ]);
    }
}
