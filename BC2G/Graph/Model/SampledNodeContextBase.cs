namespace BC2G.Graph.Model;

public class SampledNodeContextBase : ISampledNodeContext
{
    public double OriginalInDegree { get; }
    public double OriginalOutDegree { get; }

    public SampledNodeContextBase(double originalInDegree, double originalOutDegree)
    {
        OriginalInDegree = originalInDegree;
        OriginalOutDegree = originalOutDegree;
    }

    public static string[] GetFeatureNames()
    {
        return [nameof(OriginalInDegree), nameof(OriginalOutDegree)];
    }

    public string[] GetFeatures()
    {
        return [OriginalInDegree.ToString(), OriginalOutDegree.ToString()];
    }
}
