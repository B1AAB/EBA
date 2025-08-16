namespace BC2G.Graph.Model;

public interface IContext
{
    public static abstract string[] GetFeatureNames();
    public string[] GetFeatures();
}

public interface ISampledNodeContext : IContext
{
    /// <summary>
    /// This is the degree of the node in the entire graph 
    /// (e.g., the one in the database), where the Indegree and Outdegree 
    /// are the degrees in the graph where they are located (e.g., the sampled graph).
    /// </summary>
    public double OriginalInDegree { get; }

    /// <summary>
    /// This is the degree of the node in the entire graph 
    /// (e.g., the one in the database), where the Indegree and Outdegree 
    /// are the degrees in the graph where they are located (e.g., the sampled graph).
    /// </summary>
    public double OriginalOutDegree { get; }
}
