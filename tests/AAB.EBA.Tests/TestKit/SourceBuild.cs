using AAB.EBA.CLI.Config;

using System.Diagnostics;
using System.Reflection;

namespace AAB.EBA.Tests.TestKit;

/// <summary>
/// Information about the build of the source (the <c>aab.eba</c> assembly).
/// The source behaves differently in Debug builds (e.g., <c>#if DEBUG</c>
/// forces processing one block and one transaction at a time).
/// </summary>
public static class SourceBuild
{
    public static bool IsDebug { get; } =
        typeof(Options).Assembly.GetCustomAttribute<DebuggableAttribute>()?.IsJITOptimizerDisabled ?? false;
}
