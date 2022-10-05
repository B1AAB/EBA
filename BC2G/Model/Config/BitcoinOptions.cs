using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.Model.Config
{
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

        public int? FromInclusive
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
        private int? _fromInclusive = null;

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


        // TODO: Using LastProcessedBlock to determine
        // the tip of the processing queue is not very
        // accurate, because the blocks may not finish
        // in the given order. For instance, block 10
        // may finish before blocks 8 and 9. Hence
        // resuming from block 10 will lead to skipping
        // blocks 8 and 9.

        public int? LastProcessedBlock
        {
            set
            {
                _lastProcessedBlock = value;
            }
            get
            {
                if (_lastProcessedBlock is null)
                    return FromInclusive == null ? null : FromInclusive - 1;
                else
                    return _lastProcessedBlock;
            }
        }
        private int? _lastProcessedBlock = null;

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

        public bool SkipGraphLoad { get; set; }


        // Can safely handle running multiple blocks concurrently
        // with one caveat: UTXO! since multiple blocks are running
        // concurrently there is a very high chance that they will 
        // introduce conflicting changes to the database. BC2G can 
        // handle the conflicts using "optimistic update" strategy.
        // However, in most cases this will lead to many conflicts 
        // since transactions commonly refer to an output in recent
        // blocks, and resolving conflicts will slow the program 
        // such that the advantage of running multiple blocks in 
        // parallel will be minimal. A different strategy than 
        // the "optimistic update" is to ensure a utxo is persisted 
        // as soon as it is created/updated. This will lead to 
        // complicated locks cross different runs, and importantly 
        // to using a db context for a long time which is kinda
        // anti-pattern as db context is generally suggested to
        // be alive for the duration of a single db operation.
        public int MaxConcurrentBlocks { get; set; } = 1;

        // When setting this, make sure it is more than the timeout of related Resilience strategies.
        public TimeSpan HttpClientTimeout { set; get; } = TimeSpan.FromMinutes(10);

        public ResilienceStrategyOptions HttpClientResilienceStrategy { set; get; } = new();

        public ResilienceStrategyOptions BitcoinAgentResilienceStrategy { set; get; } = new()
        {
            Timeout = TimeSpan.FromMinutes(10),
            RetryCount = 3,
            MedianFirstRetryDelay = TimeSpan.FromSeconds(15),
            SamplingDuration = TimeSpan.FromMinutes(10),
            DurationOfBreak = TimeSpan.FromMinutes(1),
            FailureThreshold = 0.7,
            MinimumThroughput = 2
        };
    }
}
