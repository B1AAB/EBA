namespace BC2G.CLI.Config;

public class BitcoinOptions
{
    public Uri ClientUri
    {
        set
        {
            if (value.AbsoluteUri.EndsWith("/"))
                _clientUri = value;
            else
                _clientUri = new Uri(value.AbsoluteUri + "/");
        }
        get { return _clientUri; }
    }
    private Uri _clientUri = new("http://localhost:8332/rest/");

    public int FromInclusive
    {
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(FromInclusive),
                    "Value cannot be negative.");

            _fromInclusive = value;
        }
        get { return _fromInclusive; }
    }
    private int _fromInclusive = 0;

    public int? ToExclusive
    {
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(ToExclusive),
                    "Value cannot be negative.");

            if (value <= FromInclusive)
                throw new ArgumentOutOfRangeException(
                    nameof(ToExclusive),
                    $"Value cannot be smaller than or equal to " +
                    $"{nameof(FromInclusive)} " +
                    $"({value} is <= {FromInclusive}).");

            _toExclusive = value;
        }
        get { return _toExclusive; }
    }
    private int? _toExclusive = null;

    public int Granularity
    {
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(Granularity),
                    "Value should be greater than 0.");

            _granularity = value;
        }
        get { return _granularity; }
    }
    private int _granularity = 1;

    public string BlocksToProcessListFilename { set; get; } = string.Empty;

    public bool SkipGraphLoad { get; set; }

    public string StatsFilename { set; get; } = "bitcoin_blocks_stats.tsv";

    public int DbCommitAtUtxoBufferSize { set; get; } = 5000000;

    // null default lets runtime decide on max concurrency which is not static and changes w.r.t the load.
    public int? MaxConcurrentBlocks { get; set; } = null;

    // When setting this, make sure it is more than the timeout of related Resilience strategies.
    public TimeSpan HttpClientTimeout { set; get; } = TimeSpan.FromMinutes(10);

    public ResilienceStrategyOptions HttpClientResilienceStrategy { set; get; } = new();

    public ResilienceStrategyOptions BitcoinAgentResilienceStrategy { set; get; } = new()
    {
        Timeout = TimeSpan.FromMinutes(3),
        RetryCount = 3,
        MedianFirstRetryDelay = TimeSpan.FromSeconds(15),
        SamplingDuration = TimeSpan.FromMinutes(10),
        DurationOfBreak = TimeSpan.FromMinutes(1),
        FailureThreshold = 0.7,
        MinimumThroughput = 2
    };
}
