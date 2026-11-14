using System;
using System.IO;
using System.Threading.Tasks;

using Xunit;

namespace AAB.EBA.Tests.TestKit.Traverse;

public sealed class BaselineTraverseFixture : IAsyncLifetime
{
    private readonly string _directory = Path.Join(Path.GetTempPath(), "EBA-baseline-" + Guid.NewGuid().ToString("N"));

    public TraverseOutput Output { get; private set; } = null!;
    public TraverseWorkingDir WorkingDir { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var options = await UncanceledTraverse.RunDefaultAsync(_directory);
        WorkingDir = new TraverseWorkingDir(options);
        Output = await TraverseOutput.ReadAsync(options);
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
        return Task.CompletedTask;
    }
}
