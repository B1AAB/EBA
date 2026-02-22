namespace EBA.Blockchains.Bitcoin.GraphModel;

public static class Schema
{
    public static NodeKind[] NodeKinds
    {
        get
        {
            return
            [
                CoinbaseNode.Kind,
                BlockNode.Kind,
                TxNode.Kind,
                ScriptNode.Kind
            ];
        }
    }

    public static EdgeKind[] EdgeKinds
    {
        get
        {
            return
            [
                B2TEdge.Kind,
                C2TEdge.Kind,
                S2TEdge.Kind,
                T2SEdge.Kind,
                T2TEdge.KindTransfers,
                T2TEdge.KindFee
            ];
        }
    }
}