using EBA.Graph.Db.Neo4jDb;

namespace EBA.Blockchains.Bitcoin.GraphModel;

public class BlockNode(
    BlockMetadata blockMetadata,
    double? originalIndegree = null,
    double? originalOutdegree = null,
    double? outHopsFromRoot = null,
    string? idInGraphDb = null) : 
    Node(
        id: blockMetadata.Height.ToString(),
        originalInDegree: originalIndegree,
        originalOutDegree: originalOutdegree,
        outHopsFromRoot: outHopsFromRoot,
        idInGraphDb: idInGraphDb), 
    IComparable<BlockNode>, IEquatable<BlockNode>
{
    public static new GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinBlockNode; }
    }

    public override GraphComponentType GetGraphComponentType()
    {
        return ComponentType;
    }

    public BlockMetadata BlockMetadata { init; get; } = blockMetadata;

    public BlockNode(Block block) : this(blockMetadata: block) { }


    // TODO: all the following double-casting is because of the type
    // normalization happens when bulk-loading data into neo4j.
    // Find a better solution.

    public BlockNode(
        Neo4j.Driver.INode node,
        double originalIndegree,
        double originalOutdegree,
        double outHopsFromRoot) :
        this(
            blockMetadata: new BlockMetadata()
            {
                Hash = (string)node.Properties[Props.BlockHash.Name],
                VersionHex = (string)node.Properties[Props.BlockVersionHex.Name],
                Merkleroot = (string)node.Properties[Props.BlockMerkleroot.Name],
                Bits = (string)node.Properties[Props.BlockBits.Name],
                Chainwork = (string)node.Properties[Props.BlockChainwork.Name],
                PreviousBlockHash = (string)node.Properties[Props.BlockPreviousBlockHash.Name],
                NextBlockHash = (string)node.Properties[Props.BlockNextBlockHash.Name],
                Confirmations = (int)(long)node.Properties[Props.BlockConfirmations.Name],
                Height = (long)node.Properties[Props.Height.Name],
                Version = (ulong)(long)node.Properties[Props.BlockVersion.Name],
                Time = (uint)(long)node.Properties[Props.BlockTime.Name],
                MedianTime = (uint)(long)node.Properties[Props.BlockMedianTime.Name],
                Nonce = (ulong)(long)node.Properties[Props.BlockNonce.Name],
                TransactionsCount = (int)(long)node.Properties[Props.BlockTransactionsCount.Name],
                StrippedSize = (int)(long)node.Properties[Props.BlockStrippedSize.Name],
                Size = (int)(long)node.Properties[Props.BlockSize.Name],
                Weight = (int)(long)node.Properties[Props.BlockWeight.Name],
                CoinbaseOutputsCount = (int)(long)node.Properties[Props.BlockCoinbaseOutputsCount.Name],
                TxFees = (long)node.Properties[Props.BlockTxFees.Name],
                MintedBitcoins = (long)node.Properties[Props.BlockMintedBitcoins.Name],
                Difficulty = (double)node.Properties[Props.BlockDifficulty.Name],

                InputCounts = DescriptiveStatisticsStrategy.FromProperties(
                    node.Properties, Props.BlockInputCountsPrefix),

                OutputCounts = DescriptiveStatisticsStrategy.FromProperties(
                    node.Properties, Props.BlockOutputCountsPrefix),

                InputValues = DescriptiveStatisticsStrategy.FromProperties(
                    node.Properties, Props.BlockInputValuesPrefix),

                OutputValues = DescriptiveStatisticsStrategy.FromProperties(
                    node.Properties, Props.BlockOutputValuesPrefix),

                SpentOutputAge = DescriptiveStatisticsStrategy.FromProperties(
                    node.Properties, Props.BlockSpentOutputAgePrefix)
            },
            originalIndegree: originalIndegree,
            originalOutdegree: originalOutdegree,
            outHopsFromRoot: outHopsFromRoot,
            idInGraphDb: node.ElementId)
    { }

    public override string GetIdPropertyName()
    {
        return nameof(BlockMetadata.Height);
    }

    public new static string[] GetFeaturesName()
    {
        return 
        [
            nameof(BlockMetadata.Height),
            nameof(BlockMetadata.MedianTime),
            nameof(BlockMetadata.TransactionsCount),
            nameof(BlockMetadata.Difficulty),
            nameof(BlockMetadata.Size),
            nameof(BlockMetadata.StrippedSize),
            nameof(BlockMetadata.Confirmations),
            nameof(BlockMetadata.Weight),
            .. Node.GetFeaturesName()
        ];
    }

    public override string[] GetFeatures()
    {
        return 
        [
            BlockMetadata.Height.ToString(), 
            BlockMetadata.MedianTime.ToString(), 
            BlockMetadata.TransactionsCount.ToString(), 
            BlockMetadata.Difficulty.ToString(), 
            BlockMetadata.Size.ToString(), 
            BlockMetadata.StrippedSize.ToString(), 
            BlockMetadata.Confirmations.ToString(), 
            BlockMetadata.Weight.ToString(),
            .. base.GetFeatures()
        ];
    }

    public int CompareTo(BlockNode? other)
    {
        throw new NotImplementedException();
    }

    public bool Equals(BlockNode? other)
    {
        throw new NotImplementedException();
    }
}
