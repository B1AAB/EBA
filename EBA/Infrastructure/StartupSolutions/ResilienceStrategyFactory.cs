namespace BC2G.Infrastructure.StartupSolutions;

/// Useful links: 
/// - Circuit breaker in general: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/implement-circuit-breaker-pattern
/// - On circuit breaker configuration: https://github.com/App-vNext/Polly/wiki/Advanced-Circuit-Breaker

internal static class ResilienceStrategyFactory
{
    public static class Bitcoin
    {
        public static AsyncPolicyWrap<HttpResponseMessage> GetClientStrategy(
            IServiceProvider provider,
            ResilienceStrategyOptions options)
        {
            var retry = HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<IOException>()
                .WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(
                    retryCount: options.RetryCount,
                    medianFirstRetryDelay: options.MedianFirstRetryDelay),
                    onRetry: (outcome, timeSpan, retryAttempt, context) =>
                    {
                        // onRetry delegate is called when the policy is going
                        // to retry the user-provided delegate. After the
                        // delegate of onRetry is executed, it will wait for
                        // the amount of time given in timespan, and then it
                        // will call the user-provided delegate.
                        // See the flowchart on https://github.com/App-vNext/Polly/wiki/Retry.
                        var msg = "";
                        if (outcome.Exception != null)
                            msg = $"exception `{outcome.Exception.Message}` ";
                        if (outcome.Result != null)
                            msg += $"status code `{outcome.Result.StatusCode}`";

                        var logger = context.GetLogger();
                        if (logger != null)
                            logger.LogWarning(
                                "HttpClientPolicy: Waiting for {timespan} seconds" +
                                "before {retryAttempt} retry; " +
                                "previous attempt failed {message}",
                                timeSpan.TotalSeconds, retryAttempt, msg);
                        else
                            Console.Error.WriteLine(
                                $"HttpClientPolicy: Waiting for {timeSpan.TotalSeconds} seconds " +
                                $"before {retryAttempt} retry; " +
                                $"previous attempt failed {msg}.");
                    });

            var circuitBreaker =
                HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .AdvancedCircuitBreakerAsync(
                    failureThreshold: options.FailureThreshold,
                    samplingDuration: options.SamplingDuration,
                    minimumThroughput: options.MinimumThroughput,
                    durationOfBreak: options.DurationOfBreak,
                    onBreak: (result, timeSpan, context) =>
                    {
                        var logger = context.GetLogger();
                        var exMsg = result.Exception is null ? string.Empty : result.Exception.Message;
                        if (logger != null)
                            logger.LogWarning(
                                "HttpClientPolicy: Circuit on break; exception message: " +
                                "{exMsg}; timespan: {timeSpan} seconds.",
                                exMsg, timeSpan.TotalSeconds);
                        else
                            Console.Error.WriteLine(
                                $"HttpClientPolicy: Circuit on break; exception message: " +
                                $"{exMsg}; timespan: {timeSpan.TotalSeconds} seconds");
                    },
                    onReset: (context) =>
                    {
                        var logger = context.GetLogger();
                        if (logger != null)
                            logger.LogWarning("HttpClientPolicy: Circuit on reset");
                        else
                            Console.Error.WriteLine("HttpClientPolicy: Circuit on reset.");
                    },
                    onHalfOpen: () =>
                    {
                        Console.Error.WriteLine("HttpClientPolicy: Circuit breaker: half open");
                    });

            var timeout = Policy.TimeoutAsync<HttpResponseMessage>(
                timeout: options.Timeout,
                onTimeoutAsync: async (context, timespan, task) =>
                {
                    var logger = context.GetLogger();
                    if (logger != null)
                        logger.LogWarning(
                        "HttpClientPolicy: Timeout after waiting for {timespan} seconds on {task}.",
                        timespan.TotalSeconds, task);
                    else
                        Console.Error.WriteLine(
                            $"HttpClientPolicy: Timeout after waiting for " +
                            $"{timespan.TotalSeconds} seconds on {task}.");
                });

            var strategy = Policy.WrapAsync(retry, circuitBreaker, timeout);
            return strategy;
        }

        public static IAsyncPolicy GetGraphStrategy(ResilienceStrategyOptions options)
        {
            var retry = Policy
                .Handle<Exception>(e => e is not OperationCanceledException)
                .WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(
                    retryCount: options.RetryCount,
                    medianFirstRetryDelay: options.MedianFirstRetryDelay),
                    onRetry: (exception, timeSpan, retryAttempt, context) =>
                    {
                        var logger = context.GetLogger();
                        if (logger != null)
                            logger.LogWarning(
                                "Retry: {message} Waiting for {timespan} " +
                                "seconds before {retryAttempt} retry. Block height: {h:n0}",
                                exception.Message, timeSpan.TotalSeconds, retryAttempt,
                                context.GetBlockHeight());
                        else
                            Console.Error.WriteLine(
                                $"Retry: {exception.Message} Waiting for " +
                                $"{timeSpan.TotalSeconds} second before {retryAttempt} retry." +
                                $"Block height: {context.GetBlockHeight():n0}");
                    });

            var circuitBreaker = Policy
                .Handle<Exception>()
                .AdvancedCircuitBreakerAsync(
                    failureThreshold: options.FailureThreshold,
                    samplingDuration: options.SamplingDuration,
                    minimumThroughput: options.MinimumThroughput,
                    durationOfBreak: options.DurationOfBreak,
                    onBreak: (exception, timeSpan, context) =>
                    {
                        var logger = context.GetLogger();

                        if (logger != null)
                            logger.LogWarning(
                                "CircuitBreaker: Circuit on break; height {h:n0}; " +
                                "exception message: {exMsg}; timespan: {timeSpan}.",
                                context.GetBlockHeight(), exception.Message, timeSpan);
                        else
                            Console.Error.WriteLine(
                                $"CircuitBreaker: Circuit on break; height " +
                                $"{context.GetBlockHeight():n0}; " +
                                $"exception message: {exception.Message}; " +
                                $"timespan: {timeSpan}.");
                    },
                    onReset: (context) =>
                    {
                        var logger = context.GetLogger();
                        if (logger != null)
                            logger.LogWarning(
                                "CircuitBreaker: Reset. Height: {h:n0}", context.GetBlockHeight());
                        else
                            Console.Error.WriteLine(
                                $"CircuitBreaker: Reset. Height: {context.GetBlockHeight():n0}");
                    },
                    onHalfOpen: () =>
                    {
                        Console.WriteLine("CircuitBreaker: Half open.");
                    });

            var timeout = Policy.TimeoutAsync(
                timeout: options.Timeout,
                onTimeoutAsync: async (context, timespan, task) =>
                {
                    var logger = context.GetLogger();
                    if (logger != null)
                        logger.LogError(
                            "Timeout getting graph block height {h:n0} after {timespan} seconds. {context}",
                            context.GetBlockHeight(), timespan.TotalSeconds, context);
                    else
                        Console.Error.WriteLine(
                            $"Timeout getting graph block height {context.GetBlockHeight():n0} " +
                            $"after {timespan.TotalSeconds} seconds. {context}");
                });

            var strategy = Policy.WrapAsync(retry, circuitBreaker, timeout);

            return strategy;
        }
    }
}
