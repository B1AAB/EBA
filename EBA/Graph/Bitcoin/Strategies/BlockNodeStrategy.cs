using EBA.Graph.Bitcoin;
using EBA.Graph.Db.Neo4jDb;

namespace EBA.Graph.Bitcoin.Strategies;

public class BlockNodeStrategy(bool serializeCompressed) : StrategyBase(serializeCompressed)
{
    public const NodeLabels Label = NodeLabels.Block;

    private const Block v = null!;
    private static readonly PropertyMapping<BlockNode>[] _mappings =
    [
        PropertyMappingFactory.Height<BlockNode>(n => n.BlockMetadata.Height, p => p.GetIdFieldCsvHeader(Label.ToString())),
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
        new(nameof(v.MintedBitcoins), FieldType.Long, n => n.BlockMetadata.MintedBitcoins),

        .. PropertyMappingFactory.DescriptiveStats<BlockNode>(nameof(v.InputCounts), n => n.BlockMetadata.InputCounts),
        .. PropertyMappingFactory.DescriptiveStats<BlockNode>(nameof(v.OutputCounts), n => n.BlockMetadata.OutputCounts),
        .. PropertyMappingFactory.DescriptiveStats<BlockNode>(nameof(v.InputValues), n => n.BlockMetadata.InputValues),
        .. PropertyMappingFactory.DescriptiveStats<BlockNode>(nameof(v.OutputValues), n => n.BlockMetadata.OutputValues),
        .. PropertyMappingFactory.DescriptiveStats<BlockNode>(nameof(v.SpentOutputAge), n => n.BlockMetadata.SpentOutputAge),
        .. PropertyMappingFactory.ScriptTypeCounts<BlockNode>("Inputs", n => n.BlockMetadata.InputScriptTypeCount),
        .. PropertyMappingFactory.ScriptTypeCounts<BlockNode>("Outputs", n => n.BlockMetadata.OutputScriptTypeCount),

        new(":LABEL", FieldType.String, _ => Label, _ => ":LABEL"),
    ];

    private static readonly Dictionary<string, PropertyMapping<BlockNode>> _mappingsDict =
        _mappings.ToDictionary(m => m.Property.Name, m => m);


    public override string GetCsvHeader()
    {
        return _mappings.GetCsvHeader();
    }

    public override string GetCsvRow(IGraphElement component)
    {
        return GetCsv((BlockNode)component);
    }

    public static string GetCsv(BlockNode node)
    {
        return _mappings.GetCsv(node);
    }

    public static BlockNode Deserialize(
        Neo4j.Driver.INode node,
        double originalIndegree,
        double originalOutdegree,
        double hopsFromRoot)
    {
        var props = node.Properties;
        var blockMetadata = new BlockMetadata
        {
            Height = _mappingsDict[nameof(v.Height)].Deserialize<long>(props),
            Hash = _mappingsDict[nameof(v.Hash)].Deserialize<string>(props),
            Confirmations = _mappingsDict[nameof(v.Confirmations)].Deserialize<int>(props),
            Version = _mappingsDict[nameof(v.Version)].Deserialize<ulong>(props),
            VersionHex = _mappingsDict[nameof(v.VersionHex)].Deserialize<string>(props),
            Merkleroot = _mappingsDict[nameof(v.Merkleroot)].Deserialize<string>(props),
            Time = _mappingsDict[nameof(v.Time)].Deserialize<uint>(props),
            MedianTime = _mappingsDict[nameof(v.MedianTime)].Deserialize<uint>(props),
            Nonce = _mappingsDict[nameof(v.Nonce)].Deserialize<ulong>(props),
            Bits = _mappingsDict[nameof(v.Bits)].Deserialize<string>(props),
            Difficulty = _mappingsDict[nameof(v.Difficulty)].Deserialize<double>(props),
            Chainwork = _mappingsDict[nameof(v.Chainwork)].Deserialize<string>(props),
            TransactionsCount = _mappingsDict[nameof(v.TransactionsCount)].Deserialize<int>(props),
            PreviousBlockHash = _mappingsDict[nameof(v.PreviousBlockHash)].Deserialize<string>(props),
            NextBlockHash = _mappingsDict[nameof(v.NextBlockHash)].Deserialize<string>(props),
            StrippedSize = _mappingsDict[nameof(v.StrippedSize)].Deserialize<int>(props),
            Size = _mappingsDict[nameof(v.Size)].Deserialize<int>(props),
            Weight = _mappingsDict[nameof(v.Weight)].Deserialize<int>(props),
            CoinbaseOutputsCount = _mappingsDict[nameof(v.CoinbaseOutputsCount)].Deserialize<int>(props),
            MintedBitcoins = _mappingsDict[nameof(v.MintedBitcoins)].Deserialize<long>(props),

            InputCounts = PropertyMappingFactory.ReadDescriptiveStats(props, nameof(v.InputCounts)),
            OutputCounts = PropertyMappingFactory.ReadDescriptiveStats(props, nameof(v.OutputCounts)),
            InputValues = PropertyMappingFactory.ReadDescriptiveStats(props, nameof(v.InputValues)),
            OutputValues = PropertyMappingFactory.ReadDescriptiveStats(props, nameof(v.OutputValues)),
            SpentOutputAge = PropertyMappingFactory.ReadDescriptiveStats(props, nameof(v.SpentOutputAge)),
            InputScriptTypeCount = PropertyMappingFactory.ReadScriptTypeCounts("Inputs", props),
            OutputScriptTypeCount = PropertyMappingFactory.ReadScriptTypeCounts("Outputs", props),
            InputScriptTypeValue = PropertyMappingFactory.ReadScriptTypeCounts("Inputs", props),
            OutputScriptTypeValue = PropertyMappingFactory.ReadScriptTypeCounts("Outputs", props)
        };

        return new BlockNode(
            blockMetadata: blockMetadata,
            originalIndegree: originalIndegree,
            originalOutdegree: originalOutdegree,
            outHopsFromRoot: hopsFromRoot,
            idInGraphDb: node.ElementId);
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
}