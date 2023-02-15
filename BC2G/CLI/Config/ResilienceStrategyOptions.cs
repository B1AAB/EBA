namespace BC2G.Model.Config;

public class ResilienceStrategyOptions
{
    public TimeSpan Timeout { set; get; } = TimeSpan.FromMinutes(10);
    public int RetryCount { set; get; } = 5;
    public TimeSpan MedianFirstRetryDelay { set; get; } = TimeSpan.FromSeconds(15);

    public TimeSpan SamplingDuration { set; get; } = TimeSpan.FromMinutes(2);
    public TimeSpan DurationOfBreak { set; get; } = TimeSpan.FromMinutes(1);

    public double FailureThreshold
    {
        set
        {
            if (value < 0 || value > 1)
                throw new ArgumentOutOfRangeException(
                    nameof(FailureThreshold),
                    "Value should be between 0 and 1");
            else
                _failureThreshold = value;
        }
        get { return _failureThreshold; }
    }
    private double _failureThreshold = 0.5;

    public int MinimumThroughput
    {
        set
        {
            if (value <= 1)
                throw new ArgumentOutOfRangeException(
                    nameof(MinimumThroughput),
                    "Value should be at least 2.");
            _minimumThroughput = value;
        }
        get { return _minimumThroughput; }
    }
    private int _minimumThroughput = 2;
}
