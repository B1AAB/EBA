namespace EBA.Graph.Bitcoin.Strategies;

public class BlockNodeStrategy(bool serializeCompressed) :
    StrategyBase<BlockNode, BlockNodeStrategy>($"nodes_{BlockNode.Kind}", serializeCompressed),
    IElementSchema<BlockNode>
{
    public static string IdSpace { get; } = BlockNode.Kind.ToString();

    private const Block v = null!;

    private static readonly PropertyMapping<BlockNode>[] _economicMappings = new MappingBuilder<BlockNode>()
        .Map(n => (double?)n.BlockMetadata.RealizedCap)
        .Map(n => (double?)n.BlockMetadata.MarketCap)
        .Map(n => (double?)n.BlockMetadata.NUPL)
        .MapRange(PropertyMappingFactory.OHLCV<BlockNode>(n => n.BlockMetadata.Ohlcv))
        .ToArray();

    public static PropertyMapping<BlockNode>[] EconomicMappings { get; } = new MappingBuilder<BlockNode>()
        .MapBlockHeight(n => n.BlockMetadata.Height)
        .MapRange(_economicMappings)
        .ToArray();

    public static EntityTypeMapper<BlockNode> Mapper { get; } = new EntityTypeMapper<BlockNode>(
        new MappingBuilder<BlockNode>()
            .MapSourceId(IdSpace, n => n.BlockMetadata.Height)
            .MapBlockHeight(n => n.BlockMetadata.Height)
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

            .MapRange(PropertyMappingFactory.DescriptiveStats<BlockNode>(
                nameof(v.InputCountsStats), n => n.BlockMetadata.InputCountsStats))

            .MapRange(PropertyMappingFactory.DescriptiveStats<BlockNode>(
                nameof(v.OutputCountsStats), n => n.BlockMetadata.OutputCountsStats))

            .MapRange(PropertyMappingFactory.DescriptiveStats<BlockNode>(
                nameof(v.InputValuesStats), n => n.BlockMetadata.InputValuesStats))

            .MapRange(PropertyMappingFactory.DescriptiveStats<BlockNode>(
                nameof(v.OutputValuesStats), n => n.BlockMetadata.OutputValuesStats))

            .MapRange(PropertyMappingFactory.DescriptiveStats<BlockNode>(
                nameof(v.SpentOutputAgeStats), n => n.BlockMetadata.SpentOutputAgeStats))

            .MapRange(PropertyMappingFactory.DescriptiveStats<BlockNode>(
                nameof(v.FeesStats), n => n.BlockMetadata.FeesStats))

            .MapRange(PropertyMappingFactory.ScriptTypeCounts<BlockNode>(
                "Inputs", n => n.BlockMetadata.InputScriptTypeCount))

            .MapRange(PropertyMappingFactory.ScriptTypeCounts<BlockNode>(
                "Outputs", n => n.BlockMetadata.OutputScriptTypeCount))

            .MapRange(PropertyMappingFactory.DictionaryToColumns<BlockNode>(
                nameof(BlockNode.TripletTypeCount), Schema.EdgeKinds, n => n.TripletTypeCount))

            .MapRange(PropertyMappingFactory.DictionaryToColumns<BlockNode>(
                nameof(BlockNode.TripletTypeValueSum), Schema.EdgeKinds, n => n.TripletTypeValueSum))

            .Map(n => n.BlockMetadata.TotalSupply)
            .Map(n => n.BlockMetadata.TotalSupplyNominal)

            .MapRange(_economicMappings)

            .MapLabel(_ => BlockNode.Kind)

            .ToArray());


    // TODO: need a deserializer from string that returns only a given property,
    // so avoids building the whole object when only a few properties are needed
    // (e.g., for filtering in queries).

    public static BlockNode Deserialize(
        Neo4j.Driver.INode node,
        double? originalIndegree,
        double? originalOutdegree,
        double? hopsFromRoot)
    {
        return Deserialize(
            node.Properties,
            originalIndegree,
            originalOutdegree,
            hopsFromRoot,
            node.ElementId);
    }

    public static BlockNode Deserialize(
        IReadOnlyDictionary<string, object> props,
        double? originalIndegree = null,
        double? originalOutdegree = null,
        double? hopsFromRoot = null,
        string? idInGraphDb = null)
    {
        var blockMetadata = new BlockMetadata
        {
            Height = Mapper.GetValue(x=>x.BlockMetadata.Height, props),
            Hash = Mapper.GetValue(x=>x.BlockMetadata.Hash, props) ?? throw new InvalidDataException("Hash cannot be null"),
            Confirmations = Mapper.GetValue(x=>x.BlockMetadata.Confirmations, props),
            Version = Mapper.GetValue(x=>x.BlockMetadata.Version, props),
            VersionHex = Mapper.GetValue(x=>x.BlockMetadata.VersionHex, props),
            Merkleroot = Mapper.GetValue(x=>x.BlockMetadata.Merkleroot, props),
            Time = Mapper.GetValue(x=>x.BlockMetadata.Time, props),
            MedianTime = Mapper.GetValue(x=>x.BlockMetadata.MedianTime, props),
            Nonce = Mapper.GetValue(x=>x.BlockMetadata.Nonce, props),
            Bits = Mapper.GetValue(x=>x.BlockMetadata.Bits, props),
            Difficulty = Mapper.GetValue(x=>x.BlockMetadata.Difficulty, props),
            Chainwork = Mapper.GetValue(x=>x.BlockMetadata.Chainwork, props),
            TransactionsCount = Mapper.GetValue(x=>x.BlockMetadata.TransactionsCount, props),
            PreviousBlockHash = Mapper.GetValue(x=>x.BlockMetadata.PreviousBlockHash, props),
            NextBlockHash = Mapper.GetValue(x=>x.BlockMetadata.NextBlockHash, props),
            StrippedSize = Mapper.GetValue(x=>x.BlockMetadata.StrippedSize, props),
            Size = Mapper.GetValue(x=>x.BlockMetadata.Size, props),
            Weight = Mapper.GetValue(x=>x.BlockMetadata.Weight, props),
            CoinbaseOutputsCount = Mapper.GetValue(x=>x.BlockMetadata.CoinbaseOutputsCount, props),
            MintedBitcoins = Mapper.GetValue(x=>x.BlockMetadata.MintedBitcoins, props),
            InputCountsStats = PropertyMappingFactory.ReadDescriptiveStats(props, nameof(v.InputCountsStats)),
            OutputCountsStats = PropertyMappingFactory.ReadDescriptiveStats(props, nameof(v.OutputCountsStats)),
            InputValuesStats = PropertyMappingFactory.ReadDescriptiveStats(props, nameof(v.InputValuesStats)),
            OutputValuesStats = PropertyMappingFactory.ReadDescriptiveStats(props, nameof(v.OutputValuesStats)),
            SpentOutputAgeStats = PropertyMappingFactory.ReadDescriptiveStats(props, nameof(v.SpentOutputAgeStats)),
            FeesStats = PropertyMappingFactory.ReadDescriptiveStats(props, nameof(v.FeesStats)),
            InputScriptTypeCount = PropertyMappingFactory.ReadScriptTypeCounts("Inputs", props),
            OutputScriptTypeCount = PropertyMappingFactory.ReadScriptTypeCounts("Outputs", props),
            InputScriptTypeValue = PropertyMappingFactory.ReadScriptTypeCounts("Inputs", props),
            OutputScriptTypeValue = PropertyMappingFactory.ReadScriptTypeCounts("Outputs", props),

            TotalSupply = Mapper.GetValue(x => x.BlockMetadata.TotalSupply, props),
            TotalSupplyNominal = Mapper.GetValue(x => x.BlockMetadata.TotalSupplyNominal, props),

            RealizedCap = Mapper.GetValue(x => x.BlockMetadata.RealizedCap, props),
            Ohlcv = PropertyMappingFactory.ReadOHLCV(props)
        };

        var blockNode = new BlockNode(
            blockMetadata: blockMetadata,
            originalIndegree: originalIndegree,
            originalOutdegree: originalOutdegree,
            outHopsFromRoot: hopsFromRoot,
            idInGraphDb: idInGraphDb);

        blockNode.TripletTypeCount = PropertyMappingFactory.ReadDictionary<uint>(
            nameof(blockNode.TripletTypeCount), Schema.EdgeKinds, props);

        blockNode.TripletTypeValueSum = PropertyMappingFactory.ReadDictionary<long>(
            nameof(blockNode.TripletTypeValueSum), Schema.EdgeKinds, props);

        return blockNode;
    }

    public override string GetQuery(string filename)
    {
        // The following is an example of the generated query.
        //
        // LOAD CSV WITH HEADERS FROM 'file:///filename.csv'
        // AS line FIELDTERMINATOR '	'
        // MERGE (block:Block {Height:toInteger(line.Height)})
        // SET
        //  block.MedianTime=line.MedianTime,
        //  block.Confirmations=toInteger(line.Confirmations),
        //  block.Difficulty=toFloat(line.Difficulty),
        //  block.TransactionsCount=toInteger(line.TransactionsCount),
        //  block.Size=toInteger(line.Size),
        //  block.StrippedSize=line.StrippedSize,
        //  block.Weight=toInteger(line.Weight),
        //  block.NumGenerationEdgeTypes=toInteger(line.NumGenerationEdgeTypes),
        //  block.NumTransferEdgeTypes=toInteger(line.NumTransferEdgeTypes),
        //  block.NumChangeEdgeTypes=toInteger(line.NumChangeEdgeTypes),
        //  block.NumFeeEdgeTypes=toInteger(line.NumFeeEdgeTypes),
        //  block.SumGenerationEdgeTypes=toFloat(line.SumGenerationEdgeTypes),
        //  block.SumTransferEdgeTypes=toFloat(line.SumTransferEdgeTypes),
        //  block.SumChangeEdgeTypes=toFloat(line.SumChangeEdgeTypes),
        //  block.SumFeeEdgeTypes=toFloat(line.SumFeeEdgeTypes)
        //
        /*
        string l = Property.lineVarName, block = "block";

        var builder = new StringBuilder();
        
        builder.Append(
            $"LOAD CSV WITH HEADERS FROM '{filename}' AS {l} " +
            $"FIELDTERMINATOR '{Neo4jDbLegacy.csvDelimiter}' " +
            $"MERGE ({block}:{Label} " +
            $"{{{Props.Height.GetSetter()}}}) ");

        builder.Append("SET ");
        builder.Append(string.Join(
            ", ",
            from x in _properties where x != Props.Height select x.GetSetter(block)));
        
        return builder.ToString();
        */
        throw new NotImplementedException();
    }

    public override string[] GetSchemaConfigs()
    {
        var heightName = PropertyMappingFactory.HeightProperty.Name;
        return
        [
            $"CREATE CONSTRAINT {BlockNode.Kind}_{heightName}_Unique " +
            $"IF NOT EXISTS " +
            $"FOR (v:{BlockNode.Kind}) REQUIRE v.{heightName} IS UNIQUE"
        ];
    }
}