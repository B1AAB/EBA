using AAB.EBA.Blockchains.Bitcoin.ChainModel;
using AAB.EBA.Blockchains.Bitcoin.GraphModel;
using AAB.EBA.Graph.Model;
using AAB.EBA.Utilities;

namespace AAB.EBA.GraphDb.Tests;

public static class BitcoinGraphScenarios
{
    public static GraphBase GetCommunity1()
    {
        var g = new GraphBase();

        var b10 = new BlockNode(new Block() { Height = 10, MedianTime = 1 });
        var b20 = new BlockNode(new Block() { Height = 20, MedianTime = 2 });
        var b30 = new BlockNode(new Block() { Height = 30, MedianTime = 3 });
        var b50 = new BlockNode(new Block()
        {
            Height = 50,
            MedianTime = 5,
            TotalSupply = 100,
            Ohlcv = new OHLCV(0, 5, 9, 1, 8, 9, 7)
        });

        var s01 = new ScriptNode(address: "add1", scriptType: ScriptType.PubKey, sha256Hash: "hash1");
        var s02 = new ScriptNode(address: "add2", scriptType: ScriptType.PubKey, sha256Hash: "hash2");
        var s03 = new ScriptNode(address: "add3", scriptType: ScriptType.PubKey, sha256Hash: "hash3");
        var s04 = new ScriptNode(address: "add4", scriptType: ScriptType.PubKey, sha256Hash: "hash4");

        // the all zero values in the following are needed
        // since some tests rely on rebuilding tx node that will fail if it has null properties.
        var tx1 = new TxNode("tx1", 0, 0, 0, 0, 0);
        var tx2 = new TxNode("tx2", 0, 0, 0, 0, 0);
        var tx3 = new TxNode("tx3", 0, 0, 0, 0, 0);

        var mm = long.MaxValue;

        g.TryAddNode(b10);
        g.TryAddNode(b20);
        g.TryAddNode(b30);
        g.TryAddNode(b50);

        g.AddOrUpdateEdge(new B2TEdge(b10, tx1, height: b10.BlockMetadata.Height, value: 25));
        g.AddOrUpdateEdge(new S2TEdge(s02, tx1, creationHeight: 10, spentHeight: 20, value: 25, txid: "tx_created_1", vout: 1, generated: false));
        g.AddOrUpdateEdge(new T2SEdge(tx1, s01, creationHeight: 20, spentHeight: 50, value: 25, outputIndex: 0));

        g.AddOrUpdateEdge(new B2TEdge(b20, tx2, height: b20.BlockMetadata.Height, value: 50));
        g.AddOrUpdateEdge(new S2TEdge(s03, tx2, creationHeight: 10, spentHeight: 30, value: 50, txid: "tx_created_2", vout: 1, generated: false));
        g.AddOrUpdateEdge(new T2SEdge(tx2, s01, creationHeight: 30, spentHeight: mm, value: 50, outputIndex: 0));

        g.AddOrUpdateEdge(new B2TEdge(b30, tx3, height: b30.BlockMetadata.Height, value: 25));
        g.AddOrUpdateEdge(new S2TEdge(s01, tx3, creationHeight: 20, spentHeight: 50, value: 25, txid: "tx_created_3", vout: 0, generated: false));
        g.AddOrUpdateEdge(new T2SEdge(tx3, s04, creationHeight: 50, spentHeight: mm, value: 25, outputIndex: 0));
        return g;
    }
}