using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.CLI.Config;
using AAB.EBA.Graph.Bitcoin;
using AAB.EBA.Graph.Bitcoin.TraversalAlgorithms;
using AAB.EBA.Graph.Db;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AAB.EBA.Tests.Graph.Bitcoin.TraversalAlgorithms;

[Trait("Category", "Unit")]
public class PanoramaRootNodesFileTests : TestsBase
{
    [Theory]
    [InlineData("Coinbase", NodeKind.Coinbase, "Coinbase")]
    [InlineData("1", NodeKind.Coinbase, "Coinbase")]
    [InlineData("script", NodeKind.Script, "Address")]
    [InlineData("2", NodeKind.Script, "Address")]
    [InlineData("BLOCK", NodeKind.Block, "Height")]
    [InlineData("3", NodeKind.Block, "Height")]
    [InlineData("Tx", NodeKind.Tx, "Txid")]
    [InlineData("4", NodeKind.Tx, "Txid")]
    public async Task GetRootNodesAsync_ConcreteKind_UsesExpectedIdentifierProperty(
        string kindText, NodeKind kind, string property)
    {
        var filename = WriteRoots($" {kindText} \t value \textra\n");
        var panorama = CreatePanorama(filename);

        var root = Assert.Single(await GetRootsAsync(panorama, 1));

        Assert.Equal((kind, property, "value"), root);
    }

    [Theory]
    [InlineData("Undefined")]
    [InlineData("0")]
    [InlineData("5")]
    [InlineData("-1")]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData("Coinbase, Script")]
    public async Task GetRootNodesAsync_InvalidKind_ReportsFileAndLine(string kind)
    {
        var filename = WriteRoots($"NodeKind\tProperty\n{kind}\tvalue\n");
        var panorama = CreatePanorama(filename);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => GetRootsAsync(panorama, 1));

        Assert.Contains(filename, error.Message);
        Assert.Contains("line 2", error.Message);
        Assert.Contains("Invalid node kind", error.Message);
    }

    [Theory]
    [InlineData("Script")]
    [InlineData("Script\t")]
    [InlineData("Script\t  \textra")]
    public async Task GetRootNodesAsync_MalformedRow_ReportsFileAndLine(string row)
    {
        var filename = WriteRoots(row);
        var panorama = CreatePanorama(filename);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => GetRootsAsync(panorama, 1));

        Assert.Contains(filename, error.Message);
        Assert.Contains("line 1", error.Message);
    }

    [Fact]
    public async Task GetRootNodesAsync_HeaderCommentsAndDuplicates_PreservesUniqueKindValueOrder()
    {
        var filename = WriteRoots(
            "nodekind\tProperty\n#comment\n\nScript\tsame\n2\t same \textra\nTx\tsame\nScript\tSame\n");
        var panorama = CreatePanorama(filename);

        var roots = await GetRootsAsync(panorama, 10);

        Assert.Equal(new (NodeKind, string, string)[]
        {
            (NodeKind.Script, "Address", "same"),
            (NodeKind.Tx, "Txid", "same"),
            (NodeKind.Script, "Address", "Same")
        }, roots);
    }

    [Fact]
    public async Task GetRootNodesAsync_RepeatedAttempts_RestartsAndRereadsFile()
    {
        var filename = WriteRoots("Script\tfirst\nTx\tsecond\n");
        var panorama = CreatePanorama(filename);

        Assert.Equal((NodeKind.Script, "Address", "first"), Assert.Single(await GetRootsAsync(panorama, 1)));
        Assert.Equal((NodeKind.Script, "Address", "first"), Assert.Single(await GetRootsAsync(panorama, 1)));

        File.WriteAllText(filename, "Block\t100\n");
        Assert.Equal((NodeKind.Block, "Height", "100"), Assert.Single(await GetRootsAsync(panorama, 1)));
    }

    [Fact]
    public async Task GetRootNodesAsync_EmptyFile_ReportsFilename()
    {
        var filename = WriteRoots("Kind\tProperty\n#comment\n\n");
        var panorama = CreatePanorama(filename);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => GetRootsAsync(panorama, 1));

        Assert.Contains(filename, error.Message);
        Assert.Contains("No root nodes", error.Message);
    }

    [Fact]
    public async Task SampleAsync_MixedKinds_QueriesEachKindsIdentifierProperty()
    {
        var filename = WriteRoots("Coinbase\tCoinbase\nScript\taddress\nBlock\t100\nTx\ttxid\n");
        var graphDb = new Mock<IGraphDb<BitcoinGraph>>(MockBehavior.Strict);
        graphDb.Setup(db => db.GetNeighborhoodAsync(
                It.IsAny<NodeKind>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), 1, true, It.IsAny<CancellationToken>(), ""))
            .ReturnsAsync(new List<IRecord>());
        var panorama = CreatePanorama(filename, graphDb.Object);

        await panorama.SampleAsync(CancellationToken.None);

        foreach (var root in new (NodeKind Kind, string Property, string Id)[]
        {
            (NodeKind.Coinbase, "Coinbase", "Coinbase"),
            (NodeKind.Script, "Address", "address"),
            (NodeKind.Block, "Height", "100"),
            (NodeKind.Tx, "Txid", "txid")
        })
        {
            graphDb.Verify(db => db.GetNeighborhoodAsync(
                root.Kind, root.Property, root.Id,
                It.IsAny<int>(), 1, true, It.IsAny<CancellationToken>(), ""), Times.Once);
        }
        graphDb.VerifyNoOtherCalls();
    }

    private string WriteRoots(string content)
    {
        var filename = Path.Join(TempExeDir, "roots.tsv");
        File.WriteAllText(filename, content);
        return filename;
    }

    private Panorama CreatePanorama(string filename, IGraphDb<BitcoinGraph>? graphDb = null)
    {
        var options = new Options(TempExeDir)
        {
            Bitcoin = new BitcoinOptions
            {
                GraphSample = new BitcoinGraphSampleOptions
                {
                    RootNodesFilename = filename,
                    Count = 4,
                    MaxAttempts = 1
                }
            }
        };
        return new Panorama(options,
            graphDb ?? new Mock<IGraphDb<BitcoinGraph>>(MockBehavior.Strict).Object,
            NullLogger<BitcoinGraphOrchestrator>.Instance);
    }

    private static Task<List<(NodeKind Kind, string IdProperty, string Id)>> GetRootsAsync(Panorama panorama, int count)
    {
        var method = typeof(Panorama).GetMethod("GetRootNodesAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (Task<List<(NodeKind Kind, string IdProperty, string Id)>>)method.Invoke(
            panorama, new object[] { count, CancellationToken.None })!;
    }
}
