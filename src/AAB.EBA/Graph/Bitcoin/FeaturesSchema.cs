namespace AAB.EBA.Graph.Bitcoin;

public class FeaturesSchema
{
    public class Metadata
    {
        [JsonPropertyName("filename")]
        public string Filename { get; }

        [JsonPropertyName("kind")]
        public string Kind { get; }

        [JsonPropertyName("features")]
        public List<string> Features { get; }

        public Metadata(NodeKind kind, List<string> features)
        {
            Filename = GetElementFilename(kind);
            Kind = kind.ToString();
            Features = features;
        }

        public Metadata(EdgeKind kind, List<string> features)
        {
            Filename = GetElementFilename(kind);
            Kind = kind.ToString();
            Features = features;
        }
    }

    [JsonPropertyName("nodeTypes")]
    public Dictionary<string, Metadata> NodeTypes =>
            StaticNodeTypes.ToDictionary(k => k.Key.ToString(), v => v.Value);

    [JsonPropertyName("edgeTypes")]
    public Dictionary<string, Metadata> EdgeTypes =>
        StaticEdgeTypes.ToDictionary(k => k.Key.ToString(), v => v.Value);

    public static Dictionary<NodeKind, Metadata> StaticNodeTypes { get; } = new()
    {
        { BlockNode.Kind, new Metadata(BlockNode.Kind, ["Index", .. BlockNode.GetFeaturesName()]) },
        { TxNode.Kind, new Metadata(TxNode.Kind, ["Index", .. TxNode.GetFeaturesName()]) },
        { ScriptNode.Kind, new Metadata(ScriptNode.Kind, ["Index", .. ScriptNode.GetFeaturesName()]) },
        { CoinbaseNode.Kind, new Metadata(CoinbaseNode.Kind, ["Index", .. CoinbaseNode.GetFeaturesName()]) }
    };

    private static readonly string[] SourceAndTarget = ["Source", "Target"];
    public static Dictionary<EdgeKind, Metadata> StaticEdgeTypes { get; } = new()
    {
        { C2TEdge.Kind, new Metadata(C2TEdge.Kind, [.. SourceAndTarget, .. C2TEdge.GetFeaturesName()]) },
        { B2TEdge.Kind, new Metadata(B2TEdge.Kind, [.. SourceAndTarget, .. B2TEdge.GetFeaturesName()]) },
        { S2TEdge.Kind, new Metadata(S2TEdge.Kind, [.. SourceAndTarget, .. S2TEdge.GetFeaturesName()]) },
        { T2SEdge.Kind, new Metadata(T2SEdge.Kind, [.. SourceAndTarget, .. T2SEdge.GetFeaturesName()]) },
        { T2TEdge.KindFee, new Metadata(T2TEdge.KindFee, [.. SourceAndTarget, .. T2TEdge.GetFeaturesName()]) },
        { T2TEdge.KindTransfers, new Metadata(T2TEdge.KindTransfers, [.. SourceAndTarget, .. T2TEdge.GetFeaturesName()]) },
        { B2BEdge.Kind, new Metadata(B2BEdge.Kind, [.. SourceAndTarget, .. B2BEdge.GetFeaturesName()]) }
    };

    public static string GetElementFilename(NodeKind nodeKind)
    {
        return $"node_features_{nodeKind.ToString().ToLower().Replace('-', '_')}.tsv";
    }

    public static string GetElementFilename(EdgeKind edgeKind)
    {
        return $"edge_features_{edgeKind.ToString().ToLower().Replace('-', '_')}.tsv";
    }
}
