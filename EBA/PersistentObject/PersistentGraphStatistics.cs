
using EBA.Utilities;

namespace EBA.PersistentObject;

public class PersistentGraphStatistics(
    string filename,
    int maxObjectsPerFile,
    ILogger<PersistentGraphStatistics> logger,
    CancellationToken cT) :
    PersistentObject<BlockGraph>(
        filename,
        maxObjectsPerFile,
        logger,
        cT,
        GetSchema())
{
    public override Task SerializeAsync(BlockGraph obj, CancellationToken cT)
    {
        return base.SerializeAsync(obj, cT);
    }

    public static string GetSchema()
    {
        return string.Join(
            Options.CsvDelimiter,
            [
                "BlockHeight",
                "Confirmations",
                "MedianTime",
                "Bits",
                "Difficulty",
                "Size",
                "StrippedSize",
                "Weight",
                "TxCount",
                "MintedBitcoins",
                "TransactionFees",

                "CoinbaseOutputsCount",

                GetDescriptiveStatisticsSchema(nameof(BlockNode.BlockMetadata.InputCounts)),
                GetDescriptiveStatisticsSchema(nameof(BlockNode.BlockMetadata.OutputCounts)),
                GetDescriptiveStatisticsSchema(nameof(BlockNode.BlockMetadata.InputValues)),
                GetDescriptiveStatisticsSchema(nameof(BlockNode.BlockMetadata.OutputValues)),
                GetDescriptiveStatisticsSchema(nameof(BlockNode.BlockMetadata.SpentOutputAge)),

                string.Join(
                    Options.CsvDelimiter,
                    Enum.GetValues<ScriptType>().Select(x => $"ScriptType_{x}")),

                string.Join(
                    Options.CsvDelimiter,
                    Enum.GetValues<EdgeLabel>().Select(
                        x => "BlockGraph" + x + "EdgeCount").ToArray()),
                string.Join(
                    Options.CsvDelimiter,
                    Enum.GetValues<EdgeLabel>().Select(
                        x => "BlockGraph" + x + "EdgeValueSum").ToArray())
            ]);
    }

    private static string GetDescriptiveStatisticsSchema(string prefix)
    {
        return string.Join(
            Options.CsvDelimiter,
            new[]
            {
                $"{prefix}.{nameof(DescriptiveStatistics.Sum)}",
                $"{prefix}.{nameof(DescriptiveStatistics.Count)}",
                $"{prefix}.{nameof(DescriptiveStatistics.Min)}",
                $"{prefix}.{nameof(DescriptiveStatistics.Max)}",
                $"{prefix}.{nameof(DescriptiveStatistics.Mean)}",
                $"{prefix}.{nameof(DescriptiveStatistics.Variance)}",
                $"{prefix}.{nameof(DescriptiveStatistics.Skewness)}",
                $"{prefix}.{nameof(DescriptiveStatistics.Kurtosis)}",
                $"{prefix}.{nameof(DescriptiveStatistics.Percentiles)}.{nameof(DescriptiveStatistics.Percentile.P01)}",
                $"{prefix}.{nameof(DescriptiveStatistics.Percentiles)}.{nameof(DescriptiveStatistics.Percentile.P05)}",
                $"{prefix}.{nameof(DescriptiveStatistics.Percentiles)}.{nameof(DescriptiveStatistics.Percentile.P25)}",
                $"{prefix}.{nameof(DescriptiveStatistics.Percentiles)}.{nameof(DescriptiveStatistics.Percentile.P50)}",
                $"{prefix}.{nameof(DescriptiveStatistics.Percentiles)}.{nameof(DescriptiveStatistics.Percentile.P75)}",
                $"{prefix}.{nameof(DescriptiveStatistics.Percentiles)}.{nameof(DescriptiveStatistics.Percentile.P95)}",
                $"{prefix}.{nameof(DescriptiveStatistics.Percentiles)}.{nameof(DescriptiveStatistics.Percentile.P99)}",
            });
    }

    public static DescriptiveStatistics DeserializeDescriptiveStatistics(string value)
    {
        return null;
    }
}
