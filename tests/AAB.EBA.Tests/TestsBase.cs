using System;
using System.IO;

namespace AAB.EBA.Tests;

public class TestsBase : IDisposable
{
    public string TempExeDir { get; }

    private bool disposed = false;

    public TestsBase()
    {
        do
            TempExeDir = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString());
        while (Directory.Exists(TempExeDir));

        Directory.CreateDirectory(TempExeDir);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
                Directory.Delete(TempExeDir, true);

            disposed = true;
        }
    }
}
