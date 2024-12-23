namespace BC2G.Graph.Model;

public class GraphFeatures
{
    // TODO: make sure all the public types in the following are immutable.

    public Dictionary<GraphComponentType, List<double[]>> NodeFeatures { get; }
    public Dictionary<GraphComponentType, string[]> NodeFeaturesHeader { get; }

    public ReadOnlyCollection<double[]> EdgeFeatures { get; }
    public ReadOnlyCollection<string> EdgeFeaturesHeader { get; }

    public ReadOnlyCollection<int[]> PairIndices { get; }
    public ReadOnlyCollection<string> PairIndicesHeader { get; }

    public ReadOnlyCollection<double[]> Labels { get; }
    public ReadOnlyCollection<string> LabelsHeader { get; }

    public GraphFeatures(GraphBase graph)
    {
        var nodeFeatures = new Dictionary<GraphComponentType, List<double[]>>();
        foreach (var nodeType in new[] { GraphComponentType.BitcoinBlockNode, GraphComponentType.BitcoinTxNode, GraphComponentType.BitcoinScriptNode })
            nodeFeatures.Add(nodeType, []);
        
        NodeFeaturesHeader = [];
        NodeFeaturesHeader.Add(GraphComponentType.BitcoinBlockNode, BlockNode.GetFeaturesName());
        NodeFeaturesHeader.Add(GraphComponentType.BitcoinTxNode, TxNode.GetFeaturesName());
        NodeFeaturesHeader.Add(GraphComponentType.BitcoinScriptNode, ScriptNode.GetFeaturesName());

        var nodeIdToIdx = new Dictionary<string, int>();
        foreach (var node in graph.Nodes)
        {
            nodeFeatures[node.GetGraphComponentType()].Add(node.GetFeatures());
            nodeIdToIdx.Add(node.Id, nodeIdToIdx.Count);
        }

        var edgeFeatures = new SortedList<int[], double[]>(
            Comparer<int[]>.Create((x, y) =>
            {
                // This comparer allows duplicates,
                // and treats equal items as x greater than y.
                var r = x[0].CompareTo(y[0]);
                if (r != 0) return r;
                r = x[1].CompareTo(y[1]);
                if (r != 0) return r;
                return 1;
            }));

        foreach (var edge in graph.Edges)
        {
            edgeFeatures.Add(
                [nodeIdToIdx[edge.Source.Id], nodeIdToIdx[edge.Target.Id]],
                edge.GetFeatures());
        }

        NodeFeatures = nodeFeatures;
        EdgeFeatures = new ReadOnlyCollection<double[]>(edgeFeatures.Values);
        PairIndices = new ReadOnlyCollection<int[]>(edgeFeatures.Keys);

        Labels = new ReadOnlyCollection<double[]>(
            (from x in graph.Labels select new double[] { x })
            .ToList());

        // new ReadOnlyCollection<string>(ScriptNode.GetFeaturesName());
        EdgeFeaturesHeader = new ReadOnlyCollection<string>(S2SEdge.GetFeaturesName());
        PairIndicesHeader = new ReadOnlyCollection<string>(["SourceNodeIndex", "TargetNodeIndex"]);
        LabelsHeader = new ReadOnlyCollection<string>(["GraphOrRandomEdges"]);
    }
}
