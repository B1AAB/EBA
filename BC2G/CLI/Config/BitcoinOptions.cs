namespace BC2G.CLI.Config;

public class BitcoinOptions
{
    public Uri ClientUri
    {
        init
        {
            if (value.AbsoluteUri.EndsWith("/"))
                _clientUri = value;
            else
                _clientUri = new Uri(value.AbsoluteUri + "/");
        }
        get { return _clientUri; }
    }
    private Uri _clientUri = new("http://localhost:8332/rest/");

    public int From
    {
        init
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(From),
                    "Value cannot be negative.");
            _from = value;
        }
        get { return _from; }
    }
    private int _from = 0;

    public int? To
    {
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(To),
                    "Value cannot be negative.");

            if (value < From)
                throw new ArgumentOutOfRangeException(
                    nameof(To),
                    $"Value cannot be smaller than {nameof(From)} " +
                    $"({value} is < {From}).");

            _to = value;
        }
        get { return _to; }
    }
    private int? _to = null;

    public int Granularity
    {
        init
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

    public string BlocksToProcessListFilename { init; get; } = "bitcoin_blocks_to_process.bc2g";
    public string BlocksFailedToProcessListFilename { init; get; } = "bitcoin_blocks_failed_to_process.bc2g";

    public string StatsFilename { init; get; } = "bitcoin_blocks_stats.tsv";

    public int DbCommitAtUtxoBufferSize { init; get; } = 5000000;

    // null default lets runtime decide on max concurrency which is not static and changes w.r.t the load.
    public int? MaxConcurrentBlocks { init; get; } = null;

    // When setting this, make sure it is more than the timeout of related Resilience strategies.
    public TimeSpan HttpClientTimeout { init; get; } = TimeSpan.FromMinutes(10);

    public ResilienceStrategyOptions HttpClientResilienceStrategy { init; get; } = new();

    public ResilienceStrategyOptions BitcoinAgentResilienceStrategy { init; get; } = new()
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
