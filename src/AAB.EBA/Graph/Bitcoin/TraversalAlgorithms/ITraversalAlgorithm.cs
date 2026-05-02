namespace AAB.EBA.Graph.Bitcoin.TraversalAlgorithms;

internal interface ITraversalAlgorithm
{
    Task SampleAsync(CancellationToken ct);
}
