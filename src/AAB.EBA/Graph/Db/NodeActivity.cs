namespace AAB.EBA.Graph.Db;

/// <summary>An inclusive block-height range.</summary>
public readonly record struct BlockHeightRange(long Min, long Max);

/// <summary>
/// Activity counts of one node.
///
/// InTotal / OutTotal are the node's exact in/out degrees (from the degree store).
/// The in-range counts come from scanning at most a caller-given number of the node's
/// relationships; InScanned / OutScanned say how many relationships of each direction that
/// scan covered. When InScanned + OutScanned == InTotal + OutTotal the scan was complete
/// and the in-range counts are exact; otherwise they describe a sample of the node's edges
/// (roughly the most recently added ones, as Neo4j stores them, though that order is not
/// guaranteed).
///
/// For each requested range (same order as requested), InRange holds how many scanned
/// relationships in each direction have their activity height inside the range. Activity
/// height per relationship type: Credits -> CreationHeight, Redeems -> SpentHeight,
/// otherwise Height. Relationships with no such property count toward the scanned totals
/// only.
/// </summary>
public sealed record NodeActivityCounts(
    long InTotal,
    long OutTotal,
    long InScanned,
    long OutScanned,
    IReadOnlyList<(long In, long Out)> InRange)
{
    public bool IsComplete => InScanned + OutScanned >= InTotal + OutTotal;
}
