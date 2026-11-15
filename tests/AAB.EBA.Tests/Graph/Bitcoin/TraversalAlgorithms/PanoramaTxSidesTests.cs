using AAB.EBA.Blockchains.Bitcoin.ChainModel;
using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.CLI.Config;
using AAB.EBA.Graph.Bitcoin;
using AAB.EBA.Graph.Bitcoin.Descriptors;
using AAB.EBA.Graph.Bitcoin.TraversalAlgorithms;
using AAB.EBA.Graph.Db;
using AAB.EBA.Graph.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neo4j.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using INode = AAB.EBA.Graph.Model.INode;

namespace AAB.EBA.Tests.Graph.Bitcoin.TraversalAlgorithms;

/// <summary>
/// Tests the option ensureTxSendersAndReceivers: the selection of a sender and a receiver
/// for every expanded Tx, and the rejection of a community whose root has no counterparty.
/// </summary>
[Trait("Category", "Unit")]
public class PanoramaTxSidesTests : TestsBase
{
    private const string Tx = "tx";

    private static readonly string _valueProperty =
        T2SEdgeDescriptor.StaticMapper.GetMapping(x => x.Value).Property.Name;

    [Fact]
    public void Option_IsOnByDefaultAndReadsFromJson()
    {
        var defaults = JsonSerializer.Deserialize<BitcoinPanoramaSamplingAlgorithmOptions>("{}")!;
        var disabled = JsonSerializer.Deserialize<BitcoinPanoramaSamplingAlgorithmOptions>(
            "{\"ensureTxSendersAndReceivers\":false}")!;

        Assert.True(defaults.EnsureTxSendersAndReceivers);
        Assert.False(disabled.EnsureTxSendersAndReceivers);
    }

    [Fact]
    public void GetSendersAndReceivers_SplitsRedeemsAndCredits()
    {
        var (senders, receivers) = GetSides(
            Redeems("s1"), Redeems("s2"), Credits("r1", 10), Credits("r2", 20));

        Assert.Equal(["s1", "s2"], senders.OrderBy(x => x));
        Assert.Equal(["r1", "r2"], receivers.OrderBy(x => x));
    }

    [Fact]
    public void GetSendersAndReceivers_SenderThatIsCreditedIsNotAReceiver()
    {
        var (senders, receivers) = GetSides(Redeems("s1"), Credits("s1", 10), Credits("r1", 10));

        Assert.Equal(["s1"], senders);
        Assert.Equal(["r1"], receivers);
    }

    [Fact]
    public void GetSendersAndReceivers_CreditOfZeroIsNotAReceiver()
    {
        var (_, receivers) = GetSides(Redeems("s1"), Credits("r1", 0));

        Assert.Empty(receivers);
    }

    [Fact]
    public void GetSendersAndReceivers_IgnoresEdgesThatDoNotTouchTheTx()
    {
        var (senders, receivers) = GetSides(
            Relationship("Transfers", Tx, "otherTx"),
            Relationship("Redeems", "s1", "otherTx"),
            Relationship("Credits", "otherTx", "r1", 10));

        Assert.Empty(senders);
        Assert.Empty(receivers);
    }

    [Fact]
    public async Task FindMissingSide_PicksTheFirstOfTheSideInTheShuffledOrder()
    {
        var id = await FindMissingSide(
            side: ["s1", "s2"],
            shuffled: ["r1", "s2", "s1"],
            inGraph: [],
            kept: ["r1"]);

        Assert.Equal("s2", id);
    }

    [Fact]
    public async Task FindMissingSide_ReturnsNullWhenTheSideIsAlreadyKept()
    {
        Assert.Null(await FindMissingSide(side: ["s1", "s2"], shuffled: ["s1", "s2"], inGraph: [], kept: ["s2"]));
    }

    [Fact]
    public async Task FindMissingSide_ReturnsNullWhenTheGraphAlreadyHoldsTheSide()
    {
        Assert.Null(await FindMissingSide(side: ["s1"], shuffled: [], inGraph: ["s1"], kept: []));
    }

    [Fact]
    public async Task FindMissingSide_ReturnsNullWhenTheSideHasNoCandidate()
    {
        Assert.Null(await FindMissingSide(side: [], shuffled: ["r1"], inGraph: [], kept: []));
    }

    [Fact]
    public void RootHasCounterparty_RootSendsToAnotherScript()
    {
        var g = Graph(out var root, out var tx);
        AddRedeems(g, root, tx);
        AddCredits(g, tx, Script("other"), 10);

        Assert.True(HasCounterparty(g));
    }

    [Fact]
    public void RootHasCounterparty_RootReceivesFromAnotherScript()
    {
        var g = Graph(out var root, out var tx);
        AddRedeems(g, Script("other"), tx);
        AddCredits(g, tx, root, 10);

        Assert.True(HasCounterparty(g));
    }

    [Fact]
    public void RootHasCounterparty_FalseWhenTheTxHasNoSender()
    {
        var g = Graph(out var root, out var tx);
        AddCredits(g, tx, root, 10);
        AddCredits(g, tx, Script("other"), 10);

        Assert.False(HasCounterparty(g));
    }

    [Fact]
    public void RootHasCounterparty_FalseWhenTheTxHasNoReceiver()
    {
        var g = Graph(out var root, out var tx);
        AddRedeems(g, root, tx);
        AddRedeems(g, Script("other"), tx);

        Assert.False(HasCounterparty(g));
    }

    [Fact]
    public void RootHasCounterparty_FalseWhenTheRootOnlyGetsItsChangeBack()
    {
        var g = Graph(out var root, out var tx);
        AddRedeems(g, root, tx);
        AddRedeems(g, Script("other"), tx);
        AddCredits(g, tx, root, 10);

        Assert.False(HasCounterparty(g));
    }

    [Fact]
    public void RootHasCounterparty_FalseWhenTheRootIsOnlyPaidZero()
    {
        var g = Graph(out var root, out var tx);
        AddRedeems(g, Script("other"), tx);
        AddCredits(g, tx, root, 0);

        Assert.False(HasCounterparty(g));
    }

    [Fact]
    public void RootHasCounterparty_TrueWhenTheRootIsNotAScript()
    {
        var g = new BitcoinGraph();
        var tx = g.GetOrAddNode(new TxNode("rootTx"));
        g.AddLabel("RootNodeId", tx.Id);

        Assert.True(HasCounterparty(g));
    }

    private static BitcoinGraph Graph(out ScriptNode root, out TxNode tx)
    {
        var g = new BitcoinGraph();
        root = g.GetOrAddNode(Script("root"));
        tx = g.GetOrAddNode(new TxNode(Tx));
        g.AddLabel("RootNodeId", root.Id);
        return g;
    }

    private static ScriptNode Script(string address) => new(address, ScriptType.PubKeyHash);

    private static void AddRedeems(BitcoinGraph g, ScriptNode script, TxNode tx) =>
        g.TryGetOrAddEdge<IEdge<INode, INode>>(
            new S2TEdge(script, tx, spentHeight: 2, value: 10, txid: "prev" + script.Id, vout: 0, generated: false, creationHeight: 1),
            out _);

    private static void AddCredits(BitcoinGraph g, TxNode tx, ScriptNode script, long value) =>
        g.TryGetOrAddEdge<IEdge<INode, INode>>(
            new T2SEdge(tx, script, creationHeight: 2, value: value, outputIndex: 0),
            out _);

    private static IRelationship Redeems(string script) => Relationship("Redeems", script, Tx);

    private static IRelationship Credits(string script, long value) => Relationship("Credits", Tx, script, value);

    private static IRelationship Relationship(string type, string start, string end, long? value = null)
    {
        var properties = new Dictionary<string, object>();
        if (value.HasValue)
            properties[_valueProperty] = value.Value;

        var relationship = new Mock<IRelationship>();
        relationship.SetupGet(x => x.Type).Returns(type);
        relationship.SetupGet(x => x.StartNodeElementId).Returns(start);
        relationship.SetupGet(x => x.EndNodeElementId).Returns(end);
        relationship.SetupGet(x => x.Properties).Returns(properties);
        return relationship.Object;
    }

    private static (HashSet<string> Senders, HashSet<string> Receivers) GetSides(params IRelationship[] edges)
    {
        var method = typeof(Panorama).GetMethod("GetSendersAndReceivers", BindingFlags.NonPublic | BindingFlags.Static)!;
        return ((HashSet<string>, HashSet<string>))method.Invoke(null, [Tx, edges])!;
    }

    private static bool HasCounterparty(GraphBase g)
    {
        var method = typeof(Panorama).GetMethod("RootHasCounterparty", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (bool)method.Invoke(null, [g])!;
    }

    private async Task<string?> FindMissingSide(
        HashSet<string> side,
        List<string> shuffled,
        HashSet<string> inGraph,
        HashSet<string> kept)
    {
        var candidates = shuffled.ToDictionary(id => id, id => (INode)Script(id));
        var neighborsInGraph = inGraph.ToDictionary(id => id, id => (INode)Script(id));
        var method = typeof(Panorama).GetMethod("FindMissingSideAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var task = (Task<string?>)method.Invoke(
            CreatePanorama(),
            [side, shuffled, candidates, neighborsInGraph, kept, CancellationToken.None])!;
        return await task;
    }

    private Panorama CreatePanorama()
    {
        var options = new Options(TempExeDir)
        {
            Bitcoin = new BitcoinOptions
            {
                GraphSample = new BitcoinGraphSampleOptions
                {
                    PanoramaOptions = new BitcoinPanoramaSamplingAlgorithmOptions()
                }
            }
        };

        // A strict mock: the tests set no activity window, so the database must never be queried.
        return new Panorama(options,
            new Mock<IGraphDb<BitcoinGraph>>(MockBehavior.Strict).Object,
            NullLogger<BitcoinGraphOrchestrator>.Instance);
    }
}
