namespace AAB.EBA.Tests.TestKit.Traverse;

/// <summary>
/// The points at which a Bitcoin traverse can be canceled; see the case table
/// of TRAVERSE_CANCELLATION_TEST_PLAN.md (the case ID is given per member).
/// "K" is the height of the block the cancellation is about.
/// </summary>
public enum TraverseCancellationPoint
{
    /// <summary>C01: the token is canceled before the traverse starts.</summary>
    BeforeStart,

    /// <summary>C02: while the chain info is requested.</summary>
    WhileRequestingChainInfo,

    /// <summary>C03: after the queues are persisted, before any block is requested.</summary>
    AfterQueuesPersisted,

    /// <summary>C04: while the buffer is full (one block) and the loop waits for room.</summary>
    WhileBufferIsFull,

    /// <summary>C05: while the hash of block K is requested, nothing being written.</summary>
    WhileFetchingBlockHash,

    /// <summary>C06: while block K is requested, nothing being written.</summary>
    WhileFetchingBlockBody,

    /// <summary>C07: the hash of block K is delivered right after the cancellation.</summary>
    BlockHashDeliveredAfterCancellation,

    /// <summary>C08: block K is delivered right after the cancellation.</summary>
    BlockBodyDeliveredAfterCancellation,

    /// <summary>C09: while block K is written, and the next block is requested.</summary>
    WhileWritingBlock,

    /// <summary>C10: while block K is written, with blocks K+1 and K+2 buffered behind it.</summary>
    WhileWritingBlockWithBufferedBlocks,

    /// <summary>C11: while the last block is written, nothing queued behind it.</summary>
    WhileWritingLastBlock,

    /// <summary>C12: after the last block is fetched, blocks still queued for writing.</summary>
    AfterLastFetch,

    /// <summary>C13: while several blocks (from K) are requested concurrently.</summary>
    WhileFetchingSeveralBlocks,

    /// <summary>C14: while block K is retried after a failed request.</summary>
    WhileRetryingBlock,
}
