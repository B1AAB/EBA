using AAB.EBA.Blockchains.Bitcoin.ChainModel;
using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.Graph.Model;

namespace AAB.EBA.GraphDb.Tests;

public static class BitcoinGraphScenarios
{
    public static GraphBase GetCommunity1()
    {
        var g = new GraphBase();

        var b10 = new BlockNode(new Block() { Height = 10, MedianTime = 1 });
        var b20 = new BlockNode(new Block() { Height = 20, MedianTime = 2 });
        var b30 = new BlockNode(new Block() { Height = 30, MedianTime = 3 });
        var b50 = new BlockNode(new Block() { Height = 50, MedianTime = 5 });

        var s1 = new ScriptNode(address: "add1", scriptType: ScriptType.PubKey, sha256Hash: "hash1");
        var s2 = new ScriptNode(address: "add2", scriptType: ScriptType.PubKey, sha256Hash: "hash2");
        var s3 = new ScriptNode(address: "add3", scriptType: ScriptType.PubKey, sha256Hash: "hash3");
        var s4 = new ScriptNode(address: "add4", scriptType: ScriptType.PubKey, sha256Hash: "hash4");

        var tx1 = new TxNode("tx1");
        var tx2 = new TxNode("tx2");
        var tx3 = new TxNode("tx3");

        var mm = long.MaxValue;

        g.TryAddNode(b10);
        g.TryAddNode(b20);
        g.TryAddNode(b30);
        g.TryAddNode(b50);

        g.AddOrUpdateEdge(new S2TEdge(s2, tx1, timestamp: 0, creationHeight: 10, spentHeight: 20, value: 25, txid: "tx_created_1", vout: 1, generated: false));
        g.AddOrUpdateEdge(new T2SEdge(tx1, s1, timestamp: 0, creationHeight: 20, spentHeight: 50, value: 25, outputIndex: 0));

        g.AddOrUpdateEdge(new S2TEdge(s3, tx2, timestamp: 0, creationHeight: 10, spentHeight: 30, value: 50, txid: "tx_created_2", vout: 1, generated: false));
        g.AddOrUpdateEdge(new T2SEdge(tx2, s1, timestamp: 0, creationHeight: 30, spentHeight: mm, value: 50, outputIndex: 0));

        g.AddOrUpdateEdge(new S2TEdge(s1, tx3, timestamp: 0, creationHeight: 20, spentHeight: 50, value: 25, txid: "tx_created_3", vout: 0, generated: false));
        g.AddOrUpdateEdge(new T2SEdge(tx3, s4, timestamp: 0, creationHeight: 50, spentHeight: mm, value: 25, outputIndex: 0));
        return g;
    }
}