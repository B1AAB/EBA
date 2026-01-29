using MathNet.Numerics.Statistics;

namespace EBA.Utilities;

public class DescriptiveStatistics
{
    public class Percentile
    {
        public double P01 { init; get; } = double.NaN;
        public double P05 { init; get; } = double.NaN;
        public double P25 { init; get; } = double.NaN;
        public double P50 { init; get; } = double.NaN;
        public double P75 { init; get; } = double.NaN;
        public double P95 { init; get; } = double.NaN;
        public double P99 { init; get; } = double.NaN;

        public Percentile() { }

        public Percentile(double[] sortedSequence)
        {
            P01 = Helpers.Percentile(sortedSequence, 0.01);
            P05 = Helpers.Percentile(sortedSequence, 0.05);
            P25 = Helpers.Percentile(sortedSequence, 0.25);
            P50 = Helpers.Percentile(sortedSequence, 0.50);
            P75 = Helpers.Percentile(sortedSequence, 0.75);
            P95 = Helpers.Percentile(sortedSequence, 0.95);
            P99 = Helpers.Percentile(sortedSequence, 0.99);
        }
    }

    public double Sum { init; get; } = double.NaN;
    public double Count { init; get; } = double.NaN;
    public double Min { init; get; } = double.NaN;
    public double Max { init; get; } = double.NaN;
    public double Mean { init; get; } = double.NaN;
    public double Variance { init; get; } = double.NaN;
    public double Skewness { init; get; } = double.NaN;
    public double Kurtosis { init; get; } = double.NaN;
    public Percentile Percentiles { init; get; } = new Percentile();

    public DescriptiveStatistics() { }

    public DescriptiveStatistics(List<double> data)
    {
        if (data.Count > 0)
        {
            Sum = data.Sum();
            Count = data.Count();
            Min = data.Min();
            Max = data.Max();
            Mean = Count > 0 ? (double)Sum / Count : 0;
            Variance = Helpers.GetVariance(data);

            double[] sortedSequence = [.. data.Select(x => (double)x)];
            Array.Sort(sortedSequence);
            Percentiles = new Percentile(sortedSequence);

            Skewness = Statistics.Skewness(data);
            Kurtosis = Statistics.Kurtosis(data);
        }
    }

    public static string[] GetFeaturesName(string prefix)
    {
        return [
            $"{prefix}.{nameof(Sum)}",
            $"{prefix}.{nameof(Count)}",
            $"{prefix}.{nameof(Min)}",
            $"{prefix}.{nameof(Max)}",
            $"{prefix}.{nameof(Mean)}",
            $"{prefix}.{nameof(Variance)}",
            $"{prefix}.{nameof(Skewness)}",
            $"{prefix}.{nameof(Kurtosis)}",
            $"{prefix}.{nameof(Percentiles)}.{nameof(Percentile.P01)}",
            $"{prefix}.{nameof(Percentiles)}.{nameof(Percentile.P05)}",
            $"{prefix}.{nameof(Percentiles)}.{nameof(Percentile.P25)}",
            $"{prefix}.{nameof(Percentiles)}.{nameof(Percentile.P50)}",
            $"{prefix}.{nameof(Percentiles)}.{nameof(Percentile.P75)}",
            $"{prefix}.{nameof(Percentiles)}.{nameof(Percentile.P95)}",
            $"{prefix}.{nameof(Percentiles)}.{nameof(Percentile.P99)}"
        ];
    }

    public string[] GetFeatures()
    {
        return [
            Sum.ToString(),
            Count.ToString(),
            Min.ToString(),
            Max.ToString(),
            Mean.ToString(),
            Variance.ToString(),
            Skewness.ToString(),
            Kurtosis.ToString(),
            Percentiles.P01.ToString(),
            Percentiles.P05.ToString(),
            Percentiles.P25.ToString(),
            Percentiles.P50.ToString(),
            Percentiles.P75.ToString(),
            Percentiles.P95.ToString(),
            Percentiles.P99.ToString()
        ];
    }
}
