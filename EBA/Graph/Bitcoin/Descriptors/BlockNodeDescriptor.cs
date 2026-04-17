
namespace EBA.Graph.Bitcoin.Descriptors;

public class BlockNodeDescriptor : IElementDescriptor<BlockNode>
{
    public static string IdSpace => _idSpace;
    private static readonly string _idSpace = BlockNode.Kind.ToString();

    public ElementMapper<BlockNode> Mapper => StaticMapper;
    public static ElementMapper<BlockNode> StaticMapper => _mapper;
    private static readonly ElementMapper<BlockNode> _mapper = new(
        new MappingBuilder<BlockNode>()
            .MapSourceId(_idSpace, n => n.BlockMetadata.Height)
            .Map(n => n.BlockMetadata.Height)
            .Map(n => n.BlockMetadata.Hash)
            .Map(n => n.BlockMetadata.Confirmations)
            .Map(n => n.BlockMetadata.Version)
            .Map(n => n.BlockMetadata.VersionHex)
            .Map(n => n.BlockMetadata.Merkleroot)
            .Map(n => n.BlockMetadata.Time)
            .Map(n => n.BlockMetadata.MedianTime)
            .Map(n => n.BlockMetadata.Nonce)
            .Map(n => n.BlockMetadata.Bits)
            .Map(n => n.BlockMetadata.Difficulty)
            .Map(n => n.BlockMetadata.Chainwork)
            .Map(n => n.BlockMetadata.TransactionsCount)
            .Map(n => n.BlockMetadata.PreviousBlockHash)
            .Map(n => n.BlockMetadata.NextBlockHash)
            .Map(n => n.BlockMetadata.StrippedSize)
            .Map(n => n.BlockMetadata.Size)
            .Map(n => n.BlockMetadata.Weight)
            .Map(n => n.BlockMetadata.CoinbaseOutputsCount)
            .Map(n => n.BlockMetadata.MintedBitcoins)

            .MapRange(PropertyMappingFactory.ToMappings<BlockNode>(
                nameof(Block.InputCountsStats), n => n.BlockMetadata.InputCountsStats))

            .MapRange(PropertyMappingFactory.ToMappings<BlockNode>(
                nameof(Block.OutputCountsStats), n => n.BlockMetadata.OutputCountsStats))

            .MapRange(PropertyMappingFactory.ToMappings<BlockNode>(
                nameof(Block.InputValuesStats), n => n.BlockMetadata.InputValuesStats))

            .MapRange(PropertyMappingFactory.ToMappings<BlockNode>(
                nameof(Block.OutputValuesStats), n => n.BlockMetadata.OutputValuesStats))

            .MapRange(PropertyMappingFactory.ToMappings<BlockNode>(
                nameof(Block.SpentOutputAgeStats), n => n.BlockMetadata.SpentOutputAgeStats))

            .MapRange(PropertyMappingFactory.ToMappings<BlockNode>(
                nameof(Block.FeesStats), n => n.BlockMetadata.FeesStats))

            .MapRange(PropertyMappingFactory.ToMappings(
                "InputsCount", (BlockNode n) => n.BlockMetadata.InputScriptTypeCount))

            .MapRange(PropertyMappingFactory.ToMappings(
                "OutputsCount", (BlockNode n) => n.BlockMetadata.OutputScriptTypeCount))

            .MapRange(PropertyMappingFactory.ToMappings(
                "InputsValue", (BlockNode n) => n.BlockMetadata.InputScriptTypeValue))

            .MapRange(PropertyMappingFactory.ToMappings(
                "OutputsValue", (BlockNode n) => n.BlockMetadata.OutputScriptTypeValue))

            .MapRange(PropertyMappingFactory.ToMappings<BlockNode, uint>(
                    nameof(BlockNode.TripletTypeCount), n => n.TripletTypeCount))

            .MapRange(PropertyMappingFactory.ToMappings<BlockNode, long>(
                    nameof(BlockNode.TripletTypeValueSum), n => n.TripletTypeValueSum))

            .MapRange(PropertyMappingFactory.ToMappings(
                nameof(BlockNode.TripletTypeCount), (BlockNode n) => n.TripletTypeCount))

            .MapRange(PropertyMappingFactory.ToMappings(
                nameof(BlockNode.TripletTypeValueSum), (BlockNode n) => n.TripletTypeValueSum))

            .Map(n => n.BlockMetadata.TotalSupply)
            .Map(n => n.BlockMetadata.TotalSupplyNominal)

            .Map(n => (double?)n.BlockMetadata.RealizedCap)
            .Map(n => (double?)n.BlockMetadata.MarketCap)
            .Map(n => (double?)n.BlockMetadata.NUPL)
            .MapRange(PropertyMappingFactory.ToMappings<BlockNode>(n => n.BlockMetadata.Ohlcv))

            .MapLabel(_ => BlockNode.Kind)
            .ToArray());

    public static BlockNode Deserialize(
        IReadOnlyDictionary<string, object> props,
        double? originalIndegree = null,
        double? originalOutdegree = null,
        double? hopsFromRoot = null,
        string? idInGraphDb = null)
    {
        var blockMetadata = new BlockMetadata
        {
            Height = _mapper.GetValue(n => n.BlockMetadata.Height, props),
            Hash = _mapper.GetValue(n => n.BlockMetadata.Hash, props),
            Confirmations = _mapper.GetValue(n => n.BlockMetadata.Confirmations, props),
            Version = _mapper.GetValue(n => n.BlockMetadata.Version, props),
            VersionHex = _mapper.GetValue(n => n.BlockMetadata.VersionHex, props),
            Merkleroot = _mapper.GetValue(n => n.BlockMetadata.Merkleroot, props),
            Time = _mapper.GetValue(n => n.BlockMetadata.Time, props),
            MedianTime = _mapper.GetValue(n => n.BlockMetadata.MedianTime, props),
            Nonce = _mapper.GetValue(n => n.BlockMetadata.Nonce, props),
            Bits = _mapper.GetValue(n => n.BlockMetadata.Bits, props),
            Difficulty = _mapper.GetValue(n => n.BlockMetadata.Difficulty, props),
            Chainwork = _mapper.GetValue(n => n.BlockMetadata.Chainwork, props),
            TransactionsCount = _mapper.GetValue(n => n.BlockMetadata.TransactionsCount, props),
            PreviousBlockHash = _mapper.GetValue(n => n.BlockMetadata.PreviousBlockHash, props),
            NextBlockHash = _mapper.GetValue(n => n.BlockMetadata.NextBlockHash, props),
            StrippedSize = _mapper.GetValue(n => n.BlockMetadata.StrippedSize, props),
            Size = _mapper.GetValue(n => n.BlockMetadata.Size, props),
            Weight = _mapper.GetValue(n => n.BlockMetadata.Weight, props),
            CoinbaseOutputsCount = _mapper.GetValue(n => n.BlockMetadata.CoinbaseOutputsCount, props),
            MintedBitcoins = _mapper.GetValue(n => n.BlockMetadata.MintedBitcoins, props),
            InputCountsStats = PropertyMappingFactory.ReadDescriptiveStats(props, nameof(Block.InputCountsStats)),
            OutputCountsStats = PropertyMappingFactory.ReadDescriptiveStats(props, nameof(Block.OutputCountsStats)),
            InputValuesStats = PropertyMappingFactory.ReadDescriptiveStats(props, nameof(Block.InputValuesStats)),
            OutputValuesStats = PropertyMappingFactory.ReadDescriptiveStats(props, nameof(Block.OutputValuesStats)),
            SpentOutputAgeStats = PropertyMappingFactory.ReadDescriptiveStats(props, nameof(Block.SpentOutputAgeStats)),
            FeesStats = PropertyMappingFactory.ReadDescriptiveStats(props, nameof(Block.FeesStats)),
            InputScriptTypeCount = PropertyMappingFactory.GetDictionary<ScriptType, long>("InputsCount", props),
            OutputScriptTypeCount = PropertyMappingFactory.GetDictionary<ScriptType, long>("OutputsCount", props),
            InputScriptTypeValue = PropertyMappingFactory.GetDictionary<ScriptType, long>("InputsValue", props),
            OutputScriptTypeValue = PropertyMappingFactory.GetDictionary<ScriptType, long>("OutputsValue", props),

            TotalSupply = _mapper.GetValue(n => n.BlockMetadata.TotalSupply, props),
            TotalSupplyNominal = _mapper.GetValue(n => n.BlockMetadata.TotalSupplyNominal, props),

            RealizedCap = _mapper.GetValue(n => n.BlockMetadata.RealizedCap, props),
            Ohlcv = PropertyMappingFactory.ReadOHLCV(props)
        };

        var blockNode = new BlockNode(
            blockMetadata: blockMetadata,
            originalIndegree: originalIndegree,
            originalOutdegree: originalOutdegree,
            outHopsFromRoot: hopsFromRoot,
            idInGraphDb: idInGraphDb);

        blockNode.TripletTypeCount = PropertyMappingFactory.GetDictionary<uint>(
                nameof(blockNode.TripletTypeCount), props);

        blockNode.TripletTypeValueSum = PropertyMappingFactory.GetDictionary<long>(
            nameof(blockNode.TripletTypeValueSum), props);

        return blockNode;
    }
}