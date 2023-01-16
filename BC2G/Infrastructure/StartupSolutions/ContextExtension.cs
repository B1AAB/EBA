using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace BC2G.Infrastructure.StartupSolutions;

internal static class ContextExtension
{
    private static readonly string _loggerKey = "ILogger";

    public static Context SetLogger<T>(this Context context, ILogger logger)
    {
        context[_loggerKey] = logger;
        return context;
    }

    public static ILogger? GetLogger(this Context context)
    {
        if(context.TryGetValue(_loggerKey, out var logger))
            return logger as ILogger;
        
        return null;
    }
}
