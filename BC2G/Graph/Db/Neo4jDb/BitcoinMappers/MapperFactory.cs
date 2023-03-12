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
        /*if (type == null)
            throw new ArgumentNullException(nameof(type));*/

        return type switch
        {
            GraphComponentType.BitcoinGraph => new BlockGraphStrategy(),
            //string x when x == Utilities.TypeToString<BlockGraph>() => new BlockGraphStrategy(),
            _ => throw new MapperNotImplementedException(type)
        };
    }

    public INodeStrategy GetNodeStrategy(GraphComponentType type)
    {
        return type switch
        {
            //string x when x == Utilities.TypeToString<ScriptNode>() => new ScriptNodeStrategy(),
            GraphComponentType.BitcoinScriptNode => new ScriptNodeStrategy(),
            //string x when x == Utilities.TypeToString<TxNode>() => new TxNodeStrategy(),
            GraphComponentType.BitcoinTxNode => new TxNodeStrategy(),
            _ => throw new MapperNotImplementedException(type)
        };
    }

    public IEdgeStrategy GetEdgeStrategy(GraphComponentType type)
    {
        /*if (type == null)
            throw new ArgumentNullException(nameof(type));*/

        return type switch
        {
            //string x when x == Utilities.TypeToString<C2SEdge>() => new C2SEdgeStrategy(),
            GraphComponentType.BitcoinC2S => new C2SEdgeStrategy(),
            //string x when x == Utilities.TypeToString<C2TEdge>() => new C2TEdgeStrategy(),
            GraphComponentType.BitcoinC2T => new C2TEdgeStrategy(),
            //string x when x == Utilities.TypeToString<S2SEdge>() => new S2SEdgeStrategy(),
            GraphComponentType.BitcoinS2S => new S2SEdgeStrategy(),
            //string x when x == Utilities.TypeToString<T2TEdge>() => new T2TEdgeStrategy(),
            GraphComponentType.BitcoinT2T => new T2TEdgeStrategy(),
            _ => throw new MapperNotImplementedException(type)
        };
    }
}
