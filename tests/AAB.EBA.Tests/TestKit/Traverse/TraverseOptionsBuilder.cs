using AAB.EBA.CLI.Config;
using AAB.EBA.Serializers;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace AAB.EBA.Tests.TestKit.Traverse;

/// <summary>
/// Builds <see cref="Options"/> for running a Bitcoin traverse in tests
/// (the source options are init-only, hence the builder).
///
/// The default options resolve some files (e.g., the list of blocks to process
/// and the batches file) relative to the current directory; the options
/// built here place every file under the given working directory, so that
/// tests are isolated from each other and from the current directory.
///
/// Defaults: blocks <see cref="DefaultFromHeight"/>..<see cref="DefaultToHeight"/>,
/// one block at a time (so blocks are fetched and written in the order they
/// are requested), and no retry of a block that fails.
/// </summary>
public sealed class TraverseOptionsBuilder
{
    public const int DefaultFromHeight = 71036;
    public const int DefaultToHeight = DefaultFromHeight + 5;

    public const string BlocksToProcessFilename = "bitcoin_blocks_to_process.eba";
    public const string BlocksFailedToProcessFilename = "bitcoin_blocks_failed_to_process.eba";
    public const string BatchesFilename = "batches.json";
    public const string BlocksListFilename = "blocks_list.txt";

    private readonly string _workingDir;
    private int _from = DefaultFromHeight;
    private int _to = DefaultToHeight;
    private Uri _clientUri = new BitcoinTraverseOptions().ClientUri;
    private int _granularity = 1;
    private long[]? _blocksList;
    private long[] _previouslyFailedBlocks = [];
    private int _maxBlocksInBuffer = new BitcoinTraverseOptions().MaxBlocksInBuffer;
    private int _maxConcurrentBlocks = 1;
    private int _agentRetryCount = 0;
    private TimeSpan _agentMedianFirstRetryDelay = TimeSpan.FromMilliseconds(1);
    private int _maxEntitiesPerBatch = new Neo4jOptions().MaxEntitiesPerBatch;

    private TraverseOptionsBuilder(string workingDir)
    {
        _workingDir = workingDir;
    }

    public static TraverseOptionsBuilder ForWorkingDir(string workingDir)
    {
        return new TraverseOptionsBuilder(workingDir);
    }

    /// <summary>
    /// The heights the traverse is requested to process, in the order it
    /// processes them: the blocks of the list file (if set) or of the range,
    /// followed by the previously failed blocks.
    /// </summary>
    public IReadOnlyList<long> RequestedHeights =>
    [
        .. _blocksList ?? GetRangeHeights(),
        .. _previouslyFailedBlocks
    ];

    /// <summary>
    /// The number of blocks the traverse processes concurrently,
    /// given that Debug builds of the source process one at a time.
    /// </summary>
    public int EffectiveMaxConcurrentBlocks => SourceBuild.IsDebug ? 1 : _maxConcurrentBlocks;

    public TraverseOptionsBuilder WithRange(int from, int to)
    {
        _from = from;
        _to = to;
        return this;
    }

    public TraverseOptionsBuilder WithClientUri(Uri clientUri)
    {
        _clientUri = clientUri;
        return this;
    }

    public TraverseOptionsBuilder WithGranularity(int granularity)
    {
        _granularity = granularity;
        return this;
    }

    /// <summary>
    /// The traverse processes the given heights (written to a list file under
    /// the working directory) instead of the range.
    /// </summary>
    public TraverseOptionsBuilder WithBlocksListFile(params long[] heights)
    {
        _blocksList = heights;
        return this;
    }

    /// <summary>
    /// The list of failed blocks contains the given heights before the
    /// traverse starts, as if a previous traverse failed to process them.
    /// </summary>
    public TraverseOptionsBuilder WithPreviouslyFailedBlocks(params long[] heights)
    {
        _previouslyFailedBlocks = heights;
        return this;
    }

    public TraverseOptionsBuilder WithMaxBlocksInBuffer(int count)
    {
        _maxBlocksInBuffer = count;
        return this;
    }

    public TraverseOptionsBuilder WithMaxConcurrentBlocks(int count)
    {
        _maxConcurrentBlocks = count;
        return this;
    }

    public TraverseOptionsBuilder WithAgentRetry(int retryCount, TimeSpan medianFirstRetryDelay)
    {
        _agentRetryCount = retryCount;
        _agentMedianFirstRetryDelay = medianFirstRetryDelay;
        return this;
    }

    public TraverseOptionsBuilder WithMaxEntitiesPerBatch(int count)
    {
        _maxEntitiesPerBatch = count;
        return this;
    }

    /// <summary>
    /// Builds the options, and creates the working directory and the input
    /// files the options refer to (the list of blocks, if set, and the list
    /// of previously failed blocks, if set).
    /// </summary>
    public Options Build()
    {
        Directory.CreateDirectory(_workingDir);

        string? blocksListFile = null;
        if (_blocksList is not null)
        {
            blocksListFile = Path.Join(_workingDir, BlocksListFilename);
            File.WriteAllLines(
                blocksListFile,
                _blocksList.Select(x => x.ToString(CultureInfo.InvariantCulture)));
        }

        var failedFilename = Path.Join(_workingDir, BlocksFailedToProcessFilename);
        if (_previouslyFailedBlocks.Length > 0)
            ArraySerializer.Serialize(_previouslyFailedBlocks, failedFilename);

        return new Options(_workingDir)
        {
            Bitcoin = new BitcoinOptions
            {
                Traverse = new BitcoinTraverseOptions
                {
                    From = _from,
                    To = _to,
                    ClientUri = _clientUri,
                    Granularity = _granularity,
                    BlocksListFile = blocksListFile,
                    BlocksToProcessListFilename = Path.Join(_workingDir, BlocksToProcessFilename),
                    BlocksFailedToProcessListFilename = failedFilename,
                    MaxBlocksInBuffer = _maxBlocksInBuffer,
                    MaxConcurrentBlocks = _maxConcurrentBlocks,
                    BitcoinAgentResilienceStrategy = new ResilienceStrategyOptions
                    {
                        Timeout = TraverseRunner.DefaultTimeout,
                        RetryCount = _agentRetryCount,
                        MedianFirstRetryDelay = _agentMedianFirstRetryDelay
                    }
                }
            },
            Neo4j = new Neo4jOptions
            {
                BatchesFilename = Path.Join(_workingDir, BatchesFilename),
                MaxEntitiesPerBatch = _maxEntitiesPerBatch
            }
        };
    }

    private IEnumerable<long> GetRangeHeights()
    {
        for (long h = _from; h <= _to; h += _granularity)
            yield return h;
    }
}
