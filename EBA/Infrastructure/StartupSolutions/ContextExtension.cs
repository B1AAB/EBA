using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace EBA.Infrastructure.StartupSolutions;

internal static class ContextExtension
{
    private static readonly string _loggerKey = "ILogger";
    private static readonly string _heightKey = "Height";

    public static Context SetHeight(this Context context, long height)
    {
        context[_heightKey] = height;
        return context;
    }

    public static long? GetHeight(this Context context)
    {
        if (context.TryGetValue(_heightKey, out var h))
            return (long)h;

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
