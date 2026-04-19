
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

    public string[] Neo4jSchemaOverride
    {
        get
        {
            var height = _mapper.GetMapping(x => x.BlockMetadata.Height).Property.Name;
            return
            [
                $"\r\nCREATE CONSTRAINT {BlockNode.Kind}_{height}_Unique " +
                $"\r\nIF NOT EXISTS " +
                $"\r\nFOR (v:{BlockNode.Kind}) REQUIRE v.{height} IS UNIQUE;",
            ];
        }
    }

    public static BlockNode Deserialize(
        IReadOnlyDictionary<string, object> props,
        double? originalIndegree = null,
        double? originalOutdegree = null,
        double? hopsFromRoot = null,
        string? idInGraphDb = null)
    {
        return Deserialize(
            new ElementReader(props),
            originalIndegree, originalOutdegree, hopsFromRoot, idInGraphDb);
    }


    public static BlockNode Deserialize(string[] csvRow)
    {
        return Deserialize(
            new ElementReader<BlockNode>(csvRow, _mapper));
    }

    public static BlockNode Deserialize<TReader>(
        TReader reader,
        double? originalIndegree = null,
        double? originalOutdegree = null,
        double? hopsFromRoot = null,
        string? idInGraphDb = null)
        where TReader : IElementReader
    {
        var blockMetadata = new BlockMetadata
        {
            Height = _mapper.GetValue(n => n.BlockMetadata.Height, reader),
            Hash = _mapper.GetValue(n => n.BlockMetadata.Hash, reader),
            Confirmations = _mapper.GetValue(n => n.BlockMetadata.Confirmations, reader),
            Version = _mapper.GetValue(n => n.BlockMetadata.Version, reader),
            VersionHex = _mapper.GetValue(n => n.BlockMetadata.VersionHex, reader),
            Merkleroot = _mapper.GetValue(n => n.BlockMetadata.Merkleroot, reader),
            Time = _mapper.GetValue(n => n.BlockMetadata.Time, reader),
            MedianTime = _mapper.GetValue(n => n.BlockMetadata.MedianTime, reader),
            Nonce = _mapper.GetValue(n => n.BlockMetadata.Nonce, reader),
            Bits = _mapper.GetValue(n => n.BlockMetadata.Bits, reader),
            Difficulty = _mapper.GetValue(n => n.BlockMetadata.Difficulty, reader),
            Chainwork = _mapper.GetValue(n => n.BlockMetadata.Chainwork, reader),
            TransactionsCount = _mapper.GetValue(n => n.BlockMetadata.TransactionsCount, reader),
            PreviousBlockHash = _mapper.GetValue(n => n.BlockMetadata.PreviousBlockHash, reader),
            NextBlockHash = _mapper.GetValue(n => n.BlockMetadata.NextBlockHash, reader),
            StrippedSize = _mapper.GetValue(n => n.BlockMetadata.StrippedSize, reader),
            Size = _mapper.GetValue(n => n.BlockMetadata.Size, reader),
            Weight = _mapper.GetValue(n => n.BlockMetadata.Weight, reader),
            CoinbaseOutputsCount = _mapper.GetValue(n => n.BlockMetadata.CoinbaseOutputsCount, reader),
            MintedBitcoins = _mapper.GetValue(n => n.BlockMetadata.MintedBitcoins, reader),
            TotalSupply = _mapper.GetValue(n => n.BlockMetadata.TotalSupply, reader),
            TotalSupplyNominal = _mapper.GetValue(n => n.BlockMetadata.TotalSupplyNominal, reader),
            RealizedCap = _mapper.GetValue(n => n.BlockMetadata.RealizedCap, reader),

            InputCountsStats = PropertyMappingFactory.ReadDescriptiveStats(reader, nameof(Block.InputCountsStats)),
            OutputCountsStats = PropertyMappingFactory.ReadDescriptiveStats(reader, nameof(Block.OutputCountsStats)),
            InputValuesStats = PropertyMappingFactory.ReadDescriptiveStats(reader, nameof(Block.InputValuesStats)),
            OutputValuesStats = PropertyMappingFactory.ReadDescriptiveStats(reader, nameof(Block.OutputValuesStats)),
            SpentOutputAgeStats = PropertyMappingFactory.ReadDescriptiveStats(reader, nameof(Block.SpentOutputAgeStats)),
            FeesStats = PropertyMappingFactory.ReadDescriptiveStats(reader, nameof(Block.FeesStats)),

            InputScriptTypeCount = PropertyMappingFactory.GetDictionary<ScriptType, long>(reader, "InputsCount"),
            OutputScriptTypeCount = PropertyMappingFactory.GetDictionary<ScriptType, long>(reader, "OutputsCount"),
            InputScriptTypeValue = PropertyMappingFactory.GetDictionary<ScriptType, long>(reader, "InputsValue"),
            OutputScriptTypeValue = PropertyMappingFactory.GetDictionary<ScriptType, long>(reader, "OutputsValue"),

            Ohlcv = PropertyMappingFactory.ReadOHLCV(reader)
        };

        var blockNode = new BlockNode(
            blockMetadata: blockMetadata,
            originalIndegree: originalIndegree,
            originalOutdegree: originalOutdegree,
            outHopsFromRoot: hopsFromRoot,
            idInGraphDb: idInGraphDb);

        blockNode.TripletTypeCount = PropertyMappingFactory.GetDictionary<uint>(reader, nameof(blockNode.TripletTypeCount));
        blockNode.TripletTypeValueSum = PropertyMappingFactory.GetDictionary<long>(reader, nameof(blockNode.TripletTypeValueSum));

        return blockNode;
    }
}