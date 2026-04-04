namespace EBA.Utilities;

public static class Extensions
{
    public static IEnumerable<long> GetViewBetween(
        this long[] sortedArray, 
        long lowerBound, 
        long upperBound)
    {
        int idx = Array.BinarySearch(sortedArray, lowerBound);
        if (idx < 0) idx = ~idx;

        for (int i = idx; i < sortedArray.Length; i++)
        {
            if (sortedArray[i] >= upperBound) break;
            yield return sortedArray[i];
        }
    }
}
