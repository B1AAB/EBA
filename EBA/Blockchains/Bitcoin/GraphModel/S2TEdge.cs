namespace EBA.Blockchains.Bitcoin.GraphModel;

public class S2TEdge : Edge<ScriptNode, TxNode>
{
    public static new GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinS2T; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinS2T;
    }

    public long UTxOCreatedInBlockHeight { get; }

    public S2TEdge(
        ScriptNode source,
        TxNode target,
        long value,
        EdgeType type,
        uint timestamp,
        long blockHeight,
        long utxoCreatedInBlockHeight) :
        base(source, target, value, type, EdgeLabel.S2TTransfer, timestamp, blockHeight)
    {
        UTxOCreatedInBlockHeight = utxoCreatedInBlockHeight;
    }

    // TODO: maybe a better alternative is to override the base or get from it but now that is static
    public static new string[] GetFeaturesName()
    {
        return
        [
            nameof(Value),
            nameof(Type),
            nameof(BlockHeight),
            nameof(UTxOCreatedInBlockHeight),
            "UtxoAgeBlocks"
        ];
    }

    public override double[] GetFeatures()
    {
        return
        [
            .. base.GetFeatures(),
            UTxOCreatedInBlockHeight,
            BlockHeight - UTxOCreatedInBlockHeight
        ];
    }
}
