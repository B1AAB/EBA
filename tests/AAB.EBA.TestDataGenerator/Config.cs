namespace AAB.EBA.TestDataGenerator;

public class Config
{
    public string Command { get; set; } = string.Empty;
    public List<Interval> Intervals { get; set; } = new List<Interval>();
    public string BitcoinCoreReponses { get; set; } = string.Empty;
    public string ExpectedOutputDir { get; set; } = string.Empty;
}

public class Interval
{
    public int From { get; set; }
    public int To { get; set; }
}
