namespace BC2G.Graph.Model;

public class GraphFeatures
{
    public ReadOnlyCollection<double[]> NodeFeatures { get; }
    public ReadOnlyCollection<string> NodeFeaturesHeader { get; }

    public ReadOnlyCollection<double[]> EdgeFeatures { get; }
    public ReadOnlyCollection<string> EdgeFeaturesHeader { get; }

    public ReadOnlyCollection<int[]> PairIndices { get; }
    public ReadOnlyCollection<string> PairIndicesHeader { get; }

    public ReadOnlyCollection<double[]> Labels { get; }
    public ReadOnlyCollection<string> LabelsHeader { get; }

    public GraphFeatures(GraphBase graph)
    {
        var nodeFeatures = new List<double[]>();
        var nodeIdToIdx = new Dictionary<string, int>();
        foreach (var node in graph.Nodes)
        {
            nodeFeatures.Add(node.GetFeatures());
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
                new int[] {
                    nodeIdToIdx[edge.Source.Id],
                    nodeIdToIdx[edge.Target.Id] },
                edge.GetFeatures());
        }

        NodeFeatures = new ReadOnlyCollection<double[]>(nodeFeatures);
        EdgeFeatures = new ReadOnlyCollection<double[]>(edgeFeatures.Values);
        PairIndices = new ReadOnlyCollection<int[]>(edgeFeatures.Keys);

        Labels = new ReadOnlyCollection<double[]>(
            (from x in graph.Labels select new double[] { x })
            .ToList());

        NodeFeaturesHeader = new ReadOnlyCollection<string>(ScriptNode.GetFeaturesName());
        EdgeFeaturesHeader = new ReadOnlyCollection<string>(S2SEdge.GetFeaturesName());
        PairIndicesHeader = new ReadOnlyCollection<string>(new string[] { "SourceNodeIndex", "TargetNodeIndex" });
        LabelsHeader = new ReadOnlyCollection<string>(new string[] { "GraphOrRandomEdges" });
    }
}
