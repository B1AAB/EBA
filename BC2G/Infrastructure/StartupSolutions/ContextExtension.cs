using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace BC2G.Infrastructure.StartupSolutions;

internal static class ContextExtension
{
    private static readonly string _loggerKey = "ILogger";
    private static readonly string _blockHeightKey = "BlockHeight";

    public static Context SetBlockHeight(this Context context, int height)
    {
        context[_blockHeightKey] = height;
        return context;
    }

    public static int? GetBlockHeight(this Context context)
    {
        if (context.TryGetValue(_blockHeightKey, out var h))
            return (int)h;

        return null;
    }

    public static Context SetLogger<T>(this Context context, ILogger logger)
    {
        context[_loggerKey] = logger;
        return context;
    }

    public static ILogger? GetLogger(this Context context)
    {
        if (context.TryGetValue(_loggerKey, out var logger))
            return logger as ILogger;

        return null;
    }
}
