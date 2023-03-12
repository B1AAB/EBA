using BC2G.Exceptions;

namespace BC2G.Graph.Db.Neo4jDb.BitcoinMappers;

public class BitcoinStrategyFactory : IStrategyFactory
{
    public IStrategyBase GetStrategyBase(GraphComponentType type)
    {
        try { return GetNodeStrategy(type); }
        catch (MapperNotImplementedException) { }

        try { return GetEdgeStrategy(type); }
        catch (MapperNotImplementedException) { }

        try { return GetGraphStrategy(type); }
        catch (MapperNotImplementedException) { }

        throw new MapperNotImplementedException(type);
    }

    public IGraphStrategy GetGraphStrategy(GraphComponentType type)
    {
        return type switch
        {
            GraphComponentType.BitcoinGraph => new BlockGraphStrategy(),
            _ => throw new MapperNotImplementedException(type)
        };
    }

    public INodeStrategy GetNodeStrategy(GraphComponentType type)
    {
        return type switch
        {
            GraphComponentType.BitcoinScriptNode => new ScriptNodeStrategy(),
            GraphComponentType.BitcoinTxNode => new TxNodeStrategy(),
            _ => throw new MapperNotImplementedException(type)
        };
    }

    public IEdgeStrategy GetEdgeStrategy(GraphComponentType type)
    {
        return type switch
        {
            GraphComponentType.BitcoinC2S => new C2SEdgeStrategy(),
            GraphComponentType.BitcoinC2T => new C2TEdgeStrategy(),
            GraphComponentType.BitcoinS2S => new S2SEdgeStrategy(),
            GraphComponentType.BitcoinT2T => new T2TEdgeStrategy(),
            _ => throw new MapperNotImplementedException(type)
        };
    }
}
