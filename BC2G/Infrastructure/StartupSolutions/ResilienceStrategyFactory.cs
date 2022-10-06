using BC2G.Model.Config;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Polly.Timeout;
using Polly.Wrap;
using System.Net.Sockets;

namespace BC2G.Infrastructure.StartupSolutions
{
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

                            context.GetLogger()?.LogWarning(
                                "Waiting for {timespan} " +
                                "seconds before {retryAttempt} retry; " +
                                "previous attempt failed {message}",
                                timeSpan.TotalSeconds, retryAttempt, msg);
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
                            context.GetLogger()?.LogWarning(
                                "Circuit on break; exception message: " +
                                "{exMsg}; timespan: {timeSpan}.",
                                result.Exception.Message, timeSpan);
                        },
                        onReset: (context) =>
                        {
                            context.GetLogger()?.LogWarning(
                                "Circuit on reset");
                        },
                        onHalfOpen: () =>
                        { });

                var timeout = Policy.TimeoutAsync<HttpResponseMessage>(
                    timeout: options.Timeout,
                    onTimeoutAsync: async (context, timespan, task) =>
                    {
                        context.GetLogger()?.LogWarning(
                            "Timeout after waiting for {timespan} on {task}.",
                            timespan, task);
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
                                    "seconds before {retryAttempt} retry.}",
                                    exception.Message, timeSpan.TotalSeconds, retryAttempt);
                            else
                                Console.Error.WriteLine(
                                    $"Retry: {exception.Message} Waiting for " +
                                    $"{timeSpan.TotalSeconds} second before {retryAttempt} retry.");
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
                                    "CircuitBreaker: Circuit on break; exception message: " +
                                    "{exMsg}; timespan: {timeSpan}.",
                                    exception.Message, timeSpan);
                            else
                                Console.Error.WriteLine(
                                    $"CircuitBreaker: Circuit on break; last exception: " +
                                    $"{exception.Message}; waiting for " +
                                    $"{timeSpan.TotalSeconds} seconds");
                        },
                        onReset: (context) =>
                        {
                            var logger = context.GetLogger();
                            if (logger != null)
                                logger.LogWarning("CircuitBreaker: Reset.");
                            else
                                Console.Error.WriteLine("CircuitBreaker: Reset.");
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
                                "Timeout getting graph after {timespan} seconds. {context}",
                                timespan.TotalSeconds, context);
                        else
                            Console.Error.WriteLine(
                                $"Timeout getting graph after {timespan.TotalSeconds} seconds." +
                                $"{context}");
                    });

                var strategy = Policy.WrapAsync(retry, circuitBreaker, timeout);

                return strategy;
            }
        }
    }
}
