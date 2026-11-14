using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.CLI.Config;
using AAB.EBA.Graph.Bitcoin;
using AAB.EBA.Graph.Bitcoin.TraversalAlgorithms;
using AAB.EBA.Graph.Db;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace AAB.EBA.Tests.Graph.Bitcoin.TraversalAlgorithms;

[Trait("Category", "Unit")]
public class PanoramaSamplingTests : TestsBase
{
    [Fact]
    public void DeterministicSampling_JsonDefaultsToFalseAndAcceptsTrue()
    {
        var defaults = JsonSerializer.Deserialize<BitcoinPanoramaSamplingAlgorithmOptions>("{}")!;
        var enabled = JsonSerializer.Deserialize<BitcoinPanoramaSamplingAlgorithmOptions>("{\"deterministicSampling\":true}")!;
        var disabled = JsonSerializer.Deserialize<BitcoinPanoramaSamplingAlgorithmOptions>("{\"deterministicSampling\":false}")!;

        Assert.False(defaults.DeterministicSampling);
        Assert.True(enabled.DeterministicSampling);
        Assert.False(disabled.DeterministicSampling);
        Assert.Contains("\"deterministicSampling\":true", JsonSerializer.Serialize(enabled));
    }

    [Fact]
    public void ShuffleCandidates_DeterministicMode_ReusesExistingSeedForEachShuffle()
    {
        var panorama = CreatePanorama(new BitcoinPanoramaSamplingAlgorithmOptions { DeterministicSampling = true });
        var candidates = Enumerable.Range(0, 30).Select(i => i.ToString()).ToArray();
        var oldRandom = new Random(31);
        var expected = candidates.OrderBy(_ => oldRandom.Next()).ToArray();

        Assert.Equal(expected, Shuffle(panorama, candidates));
        Assert.Equal(expected, Shuffle(panorama, candidates));
        Assert.Equal(expected, Shuffle(CreatePanorama(new BitcoinPanoramaSamplingAlgorithmOptions { DeterministicSampling = true }), candidates));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"deterministicSampling\":false}")]
    public void ShuffleCandidates_NonDeterministicMode_CanProduceDifferentOrders(string json)
    {
        var panorama = CreatePanorama(JsonSerializer.Deserialize<BitcoinPanoramaSamplingAlgorithmOptions>(json)!);
        var candidates = Enumerable.Range(0, 30).Select(i => i.ToString()).ToArray();
        var samples = Enumerable.Range(0, 4).Select(_ => Shuffle(panorama, candidates)).ToArray();

        Assert.All(samples, sample => Assert.Equal(candidates.OrderBy(x => x), sample.OrderBy(x => x)));
        Assert.Contains(samples.Skip(1), sample => !sample.SequenceEqual(samples[0]));
    }

    private Panorama CreatePanorama(BitcoinPanoramaSamplingAlgorithmOptions panoramaOptions)
    {
        var options = new Options(TempExeDir)
        {
            Bitcoin = new BitcoinOptions
            {
                GraphSample = new BitcoinGraphSampleOptions { PanoramaOptions = panoramaOptions }
            }
        };
        return new Panorama(options,
            new Mock<IGraphDb<BitcoinGraph>>(MockBehavior.Strict).Object,
            NullLogger<BitcoinGraphOrchestrator>.Instance);
    }

    private static List<string> Shuffle(Panorama panorama, IEnumerable<string> candidates)
    {
        var method = typeof(Panorama).GetMethod("ShuffleCandidates", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (List<string>)method.Invoke(panorama, new object[] { candidates })!;
    }
}
