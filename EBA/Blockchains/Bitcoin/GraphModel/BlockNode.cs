using EBA.Graph.Db.Neo4jDb;
using EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

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

    public uint[] EdgeLabelCount { set; get; } = [];
    public long[] EdgeLabelValueSum { set; get; } = [];

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
            blockMetadata: ReadBlockMetadata(node.Properties),
            originalIndegree: originalIndegree,
            originalOutdegree: originalOutdegree,
            outHopsFromRoot: outHopsFromRoot,
            idInGraphDb: node.ElementId)
    { }

    private static BlockMetadata ReadBlockMetadata(IReadOnlyDictionary<string, object> props)
    {
        Block b; // dummy for nameof

        return new BlockMetadata
        {
            Hash = (string)props[nameof(b.Hash)],
            VersionHex = (string)props[nameof(b.VersionHex)],
            Merkleroot = (string)props[nameof(b.Merkleroot)],
            Bits = (string)props[nameof(b.Bits)],
            Chainwork = (string)props[nameof(b.Chainwork)],
            PreviousBlockHash = (string)props[nameof(b.PreviousBlockHash)],
            NextBlockHash = (string)props[nameof(b.NextBlockHash)],
            Confirmations = (int)(long)props[nameof(b.Confirmations)],
            Height = long.Parse((string)props[nameof(b.Height)]),
            Version = (ulong)(long)props[nameof(b.Version)],
            Time = (uint)(long)props[nameof(b.Time)],
            MedianTime = (uint)(long)props[nameof(b.MedianTime)],
            Nonce = (ulong)(long)props[nameof(b.Nonce)],
            TransactionsCount = (int)(long)props[nameof(b.TransactionsCount)],
            StrippedSize = (int)(long)props[nameof(b.StrippedSize)],
            Size = (int)(long)props[nameof(b.Size)],
            Weight = (int)(long)props[nameof(b.Weight)],
            CoinbaseOutputsCount = (int)(long)props[nameof(b.CoinbaseOutputsCount)],
            TxFees = (long)props[nameof(b.TxFees)],
            MintedBitcoins = (long)props[nameof(b.MintedBitcoins)],
            Difficulty = (double)props[nameof(b.Difficulty)],
            InputCounts = MappingHelpers.ReadDescriptiveStats(props, nameof(b.InputCounts)),
            OutputCounts = MappingHelpers.ReadDescriptiveStats(props, nameof(b.OutputCounts)),
            InputValues = MappingHelpers.ReadDescriptiveStats(props, nameof(b.InputValues)),
            OutputValues = MappingHelpers.ReadDescriptiveStats(props, nameof(b.OutputValues)),
            SpentOutputAge = MappingHelpers.ReadDescriptiveStats(props, nameof(b.SpentOutputAge)),
            ScriptTypeCount = MappingHelpers.ReadScriptTypeCounts(props)
        };
    }

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
