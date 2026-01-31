using EBA.Graph.Bitcoin;

namespace EBA.Graph.Db.Neo4jDb.Bitcoin.Strategies;

public class BlockNodeStrategy(bool serializeCompressed) : StrategyBase(serializeCompressed)
{
    public const NodeLabels Label = NodeLabels.Block;

    private const Block v = null!;
    private static readonly PropertyMapping<BlockNode>[] _mappings =
    [
        new(nameof(v.Height), FieldType.Int, n => n.BlockMetadata.Height, p => p.GetIdFieldCsvHeader(Label.ToString())),
        new(nameof(v.Hash), FieldType.String, n => n.BlockMetadata.Hash),
        new(nameof(v.Confirmations), FieldType.Long, n => n.BlockMetadata.Confirmations),
        new(nameof(v.Version), FieldType.Long, n => n.BlockMetadata.Version),
        new(nameof(v.VersionHex), FieldType.String, n => n.BlockMetadata.VersionHex),
        new(nameof(v.Merkleroot), FieldType.String, n => n.BlockMetadata.Merkleroot),
        new(nameof(v.Time), FieldType.Long, n => n.BlockMetadata.Time),
        new(nameof(v.MedianTime), FieldType.Long, n => n.BlockMetadata.MedianTime),
        new(nameof(v.Nonce), FieldType.Long, n => n.BlockMetadata.Nonce),
        new(nameof(v.Bits), FieldType.String, n => n.BlockMetadata.Bits),
        new(nameof(v.Difficulty), FieldType.Double, n => n.BlockMetadata.Difficulty),
        new(nameof(v.Chainwork), FieldType.String, n => n.BlockMetadata.Chainwork),
        new(nameof(v.TransactionsCount), FieldType.Long, n => n.BlockMetadata.TransactionsCount),
        new(nameof(v.PreviousBlockHash), FieldType.String, n => n.BlockMetadata.PreviousBlockHash),
        new(nameof(v.NextBlockHash), FieldType.String, n => n.BlockMetadata.NextBlockHash),
        new(nameof(v.StrippedSize), FieldType.Long, n => n.BlockMetadata.StrippedSize),
        new(nameof(v.Size), FieldType.Long, n => n.BlockMetadata.Size),
        new(nameof(v.Weight), FieldType.Long, n => n.BlockMetadata.Weight),
        new(nameof(v.CoinbaseOutputsCount), FieldType.Long, n => n.BlockMetadata.CoinbaseOutputsCount),
        new(nameof(v.TxFees), FieldType.Long, n => n.BlockMetadata.TxFees),
        new(nameof(v.MintedBitcoins), FieldType.Long, n => n.BlockMetadata.MintedBitcoins),

        .. MappingHelpers.DescriptiveStats<BlockNode>(nameof(v.InputCounts), n => n.BlockMetadata.InputCounts),
        .. MappingHelpers.DescriptiveStats<BlockNode>(nameof(v.OutputCounts), n => n.BlockMetadata.OutputCounts),
        .. MappingHelpers.DescriptiveStats<BlockNode>(nameof(v.InputValues), n => n.BlockMetadata.InputValues),
        .. MappingHelpers.DescriptiveStats<BlockNode>(nameof(v.OutputValues), n => n.BlockMetadata.OutputValues),
        .. MappingHelpers.DescriptiveStats<BlockNode>(nameof(v.SpentOutputAge), n => n.BlockMetadata.SpentOutputAge),
        .. MappingHelpers.ScriptTypeCounts<BlockNode>(n => n.BlockMetadata.ScriptTypeCount),

        new(":LABEL", FieldType.String, _ => Label, _ => ":LABEL"),
    ];

    private static readonly Dictionary<string, PropertyMapping<BlockNode>> _mappingsDict =
        _mappings.ToDictionary(m => m.Property.Name, m => m);


    public override string GetCsvHeader()
    {
        return _mappings.GetCsvHeader();
    }

    public override string GetCsv(IGraphComponent component)
    {
        return GetCsv((BlockNode)component);
    }

    public static string GetCsv(BlockNode node)
    {
        return _mappings.GetCsv(node);
    }

    public static BlockMetadata GetNodeFromProps(IReadOnlyDictionary<string, object> props)
    {
        return new BlockMetadata
        {
            Height = _mappingsDict[nameof(v.Height)].ReadFrom<long>(props),
            Hash = _mappingsDict[nameof(v.Hash)].ReadFrom<string>(props),
            Confirmations = _mappingsDict[nameof(v.Confirmations)].ReadFrom<int>(props),
            Version = _mappingsDict[nameof(v.Version)].ReadFrom<ulong>(props),
            VersionHex = _mappingsDict[nameof(v.VersionHex)].ReadFrom<string>(props),
            Merkleroot = _mappingsDict[nameof(v.Merkleroot)].ReadFrom<string>(props),
            Time = _mappingsDict[nameof(v.Time)].ReadFrom<uint>(props),
            MedianTime = _mappingsDict[nameof(v.MedianTime)].ReadFrom<uint>(props),
            Nonce = _mappingsDict[nameof(v.Nonce)].ReadFrom<ulong>(props),
            Bits = _mappingsDict[nameof(v.Bits)].ReadFrom<string>(props),
            Difficulty = _mappingsDict[nameof(v.Difficulty)].ReadFrom<double>(props),
            Chainwork = _mappingsDict[nameof(v.Chainwork)].ReadFrom<string>(props),
            TransactionsCount = _mappingsDict[nameof(v.TransactionsCount)].ReadFrom<int>(props),
            PreviousBlockHash = _mappingsDict[nameof(v.PreviousBlockHash)].ReadFrom<string>(props),
            NextBlockHash = _mappingsDict[nameof(v.NextBlockHash)].ReadFrom<string>(props),
            StrippedSize = _mappingsDict[nameof(v.StrippedSize)].ReadFrom<int>(props),
            Size = _mappingsDict[nameof(v.Size)].ReadFrom<int>(props),
            Weight = _mappingsDict[nameof(v.Weight)].ReadFrom<int>(props),
            CoinbaseOutputsCount = _mappingsDict[nameof(v.CoinbaseOutputsCount)].ReadFrom<int>(props),
            TxFees = _mappingsDict[nameof(v.TxFees)].ReadFrom<long>(props),
            MintedBitcoins = _mappingsDict[nameof(v.MintedBitcoins)].ReadFrom<long>(props),

            InputCounts = MappingHelpers.ReadDescriptiveStats(props, nameof(v.InputCounts)),
            OutputCounts = MappingHelpers.ReadDescriptiveStats(props, nameof(v.OutputCounts)),
            InputValues = MappingHelpers.ReadDescriptiveStats(props, nameof(v.InputValues)),
            OutputValues = MappingHelpers.ReadDescriptiveStats(props, nameof(v.OutputValues)),
            SpentOutputAge = MappingHelpers.ReadDescriptiveStats(props, nameof(v.SpentOutputAge)),
            ScriptTypeCount = MappingHelpers.ReadScriptTypeCounts(props)
        };
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

        string l = Property.lineVarName, block = "block";

        var builder = new StringBuilder();
        /*
        builder.Append(
            $"LOAD CSV WITH HEADERS FROM '{filename}' AS {l} " +
            $"FIELDTERMINATOR '{Neo4jDbLegacy.csvDelimiter}' " +
            $"MERGE ({block}:{Label} " +
            $"{{{Props.Height.GetSetter()}}}) ");

        builder.Append("SET ");
        builder.Append(string.Join(
            ", ",
            from x in _properties where x != Props.Height select x.GetSetter(block)));
        */
        return builder.ToString();
    }
}