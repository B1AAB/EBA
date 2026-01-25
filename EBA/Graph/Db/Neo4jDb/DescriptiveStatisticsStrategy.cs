using EBA.Utilities;

namespace EBA.Graph.Db.Neo4jDb;

public class DescriptiveStatisticsStrategy(bool serializeCompressed) : StrategyBase(serializeCompressed)
{
    public override string GetCsv(IGraphComponent component)
    {
        throw new NotImplementedException();
    }

    public override string GetCsvHeader()
    {
        throw new NotImplementedException();
    }

    public static string GetCsvHeader(string prefix)
    {
        var x = new Props.DescriptiveStatisticsProperties(prefix);

        return string.Join(
            Options.CsvDelimiter,
            [
                x.Sum.TypeAnnotatedCsvHeader,
                x.Count.TypeAnnotatedCsvHeader,
                x.Min.TypeAnnotatedCsvHeader,
                x.Max.TypeAnnotatedCsvHeader,
                x.Mean.TypeAnnotatedCsvHeader,
                x.Variance.TypeAnnotatedCsvHeader,
                x.Skewness.TypeAnnotatedCsvHeader,
                x.Kurtosis.TypeAnnotatedCsvHeader,
                x.Percentile_P01.TypeAnnotatedCsvHeader,
                x.Percentile_P05.TypeAnnotatedCsvHeader,
                x.Percentile_P25.TypeAnnotatedCsvHeader,
                x.Percentile_P50.TypeAnnotatedCsvHeader,
                x.Percentile_P75.TypeAnnotatedCsvHeader,
                x.Percentile_P95.TypeAnnotatedCsvHeader,
                x.Percentile_P99.TypeAnnotatedCsvHeader
            ]);
    }

    public static string GetCsv(DescriptiveStatistics statistics)
    {
        return string.Join(
            Options.CsvDelimiter,
            [
                statistics.Sum.ToString(),
                statistics.Count.ToString(),
                statistics.Min.ToString(),
                statistics.Max.ToString(),
                statistics.Mean.ToString(),
                statistics.Variance.ToString(),
                statistics.Skewness.ToString(),
                statistics.Kurtosis.ToString(),
                statistics.Percentiles.P01.ToString(),
                statistics.Percentiles.P05.ToString(),
                statistics.Percentiles.P25.ToString(),
                statistics.Percentiles.P50.ToString(),
                statistics.Percentiles.P75.ToString(),
                statistics.Percentiles.P95.ToString(),
                statistics.Percentiles.P99.ToString()
            ]);
    }

    public static DescriptiveStatistics FromProperties(
        IReadOnlyDictionary<string, object> nodeProperties, 
        string prefix)
    {
        var statsProps = new Props.DescriptiveStatisticsProperties(prefix);

        return new DescriptiveStatistics()
        {
            Sum = (float)nodeProperties[statsProps.Sum.Name],
            Count = (float)nodeProperties[statsProps.Count.Name],
            Min = (float)nodeProperties[statsProps.Min.Name],
            Max = (float)nodeProperties[statsProps.Max.Name],
            Mean = (float)nodeProperties[statsProps.Mean.Name],
            Variance = (float)nodeProperties[statsProps.Variance.Name],
            Skewness = (float)nodeProperties[statsProps.Skewness.Name],
            Kurtosis = (float)nodeProperties[statsProps.Kurtosis.Name],
            Percentiles = new DescriptiveStatistics.Percentile()
            {
                P01 = (float)nodeProperties[statsProps.Percentile_P01.Name],
                P05 = (float)nodeProperties[statsProps.Percentile_P05.Name],
                P25 = (float)nodeProperties[statsProps.Percentile_P25.Name],
                P50 = (float)nodeProperties[statsProps.Percentile_P50.Name],
                P75 = (float)nodeProperties[statsProps.Percentile_P75.Name],
                P95 = (float)nodeProperties[statsProps.Percentile_P95.Name],
                P99 = (float)nodeProperties[statsProps.Percentile_P99.Name]
            }
        };
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
    }
}
