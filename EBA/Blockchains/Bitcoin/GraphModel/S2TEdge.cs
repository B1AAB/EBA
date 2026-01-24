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

    public EdgeLabel Label { get { return EdgeLabel.S2TTransfer; } }

    public long UTxOCreatedInBockHeight { get; }

    public S2TEdge(
        ScriptNode source,
        TxNode target,
        long value,
        EdgeType type,
        uint timestamp,
        long blockHeight,
        long utxoCreatedInBlockHeight) :
        base(source, target, value, type, timestamp, blockHeight)
    {
        UTxOCreatedInBockHeight = utxoCreatedInBlockHeight;
    }

    public S2TEdge Update(long value)
    {
        return new S2TEdge(Source, Target, Value + value, Type, Timestamp, BlockHeight, UTxOCreatedInBockHeight);
    }


    // TODO: maybe a better alternative is to override the base or get from it but now that is static
    public static new string[] GetFeaturesName()
    {
        return
        [
            nameof(Value),
            nameof(Type),
            nameof(BlockHeight),
            nameof(UTxOCreatedInBockHeight),
            "UtxoAgeBlocks"
        ];
    }

    public override double[] GetFeatures()
    {
        return
        [
            .. base.GetFeatures(),
            UTxOCreatedInBockHeight,
            BlockHeight - UTxOCreatedInBockHeight
        ];
    }
}
