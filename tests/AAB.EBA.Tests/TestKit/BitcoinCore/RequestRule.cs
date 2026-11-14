using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AAB.EBA.Tests.TestKit.BitcoinCore;

/// <summary>
/// Changes how a <see cref="SyntheticBitcoinNode"/> answers the requests for an
/// endpoint, for the blocks at or above a height (or for all the requests to
/// that endpoint when no height is given). The behaviours compose in this
/// order: <see cref="Fail"/>, then <see cref="Before"/>, then <see cref="Stall"/>;
/// a request that is not failed, stalled, or delayed is answered normally.
/// </summary>
public sealed class RequestRule
{
    private int _failuresLeft;
    private Func<long?, Task>? _before;
    private StalledRequest? _stall;

    internal RequestRule(BitcoinCoreEndpoint endpoint, long? fromHeight)
    {
        Endpoint = endpoint;
        FromHeight = fromHeight;
    }

    public BitcoinCoreEndpoint Endpoint { get; }
    public long? FromHeight { get; }

    /// <summary>
    /// The first <paramref name="times"/> matching requests fail with an
    /// <see cref="HttpRequestException"/>; later requests fall through to the
    /// rest of the rule.
    /// </summary>
    public RequestRule Fail(int times = int.MaxValue)
    {
        Volatile.Write(ref _failuresLeft, times);
        return this;
    }

    /// <summary>
    /// Matching requests are not answered until they are canceled.
    /// </summary>
    public StalledRequest Stall()
    {
        _stall = new StalledRequest();
        return _stall;
    }

    /// <summary>
    /// <paramref name="hook"/> is awaited before a matching request is answered
    /// (with the height of the requested block); the response is delivered as
    /// soon as the hook returns.
    /// </summary>
    public void Before(Func<long?, Task> hook)
    {
        _before = hook;
    }

    internal bool Matches(BitcoinCoreEndpoint endpoint, long? height)
    {
        return endpoint == Endpoint && (FromHeight is null || height >= FromHeight);
    }

    internal async Task ApplyAsync(BitcoinCoreRequest request, CancellationToken ct)
    {
        if (TryConsumeFailure())
            throw new HttpRequestException(
                $"Synthetic failure of the request to {request.Endpoint} for block {request.Height}.");

        if (_before is not null)
            await _before(request.Height);

        if (_stall is not null)
            await _stall.StallUntilCanceledAsync(ct);
    }

    private bool TryConsumeFailure()
    {
        while (true)
        {
            var left = Volatile.Read(ref _failuresLeft);
            if (left <= 0)
                return false;

            if (Interlocked.CompareExchange(ref _failuresLeft, left - 1, left) == left)
                return true;
        }
    }
}
