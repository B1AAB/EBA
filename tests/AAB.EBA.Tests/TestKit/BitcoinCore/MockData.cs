using AAB.EBA.TestDataGenerator;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AAB.EBA.Tests.TestKit.BitcoinCore;

public static class MockData
{
    public static string DirectoryPath => Path.Combine(AppContext.BaseDirectory, Dirs.MockData);
    public static string BitcoinCoreResponsesDir => Path.Combine(DirectoryPath, Dirs.Bitcoin, Dirs.BitcoinCoreResponses);
    public static string ExpectedOutputsDir => Path.Combine(DirectoryPath, Dirs.Bitcoin, Dirs.ExpectedOutputs);
    public static IReadOnlyDictionary<long, string> HeightToHash { get; } =
        JsonSerializer.Deserialize<Dictionary<long, string>>(File.ReadAllText(Path.Combine(BitcoinCoreResponsesDir, Files.HeightToHashMapFilename)))
        ?? throw new InvalidDataException("Invalid height-to-hash mapping.");

    public static string ChainInfo() => File.ReadAllText(Path.Combine(BitcoinCoreResponsesDir, Files.ChainInfo));
    public static string BlockJson(long height) => BlockJson(HeightToHash[height]);
    public static string BlockJson(string hash) => File.ReadAllText(Path.Combine(BitcoinCoreResponsesDir, Files.GetMockBlockFilename(hash)));
}
