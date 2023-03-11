using BC2G.Exceptions;

namespace BC2G.Graph.Db.Neo4jDb.BitcoinMappers;

public class BitcoinStrategyFactory : IStrategyFactory
{
    public IStrategyBase GetStrategyBase(string type)
    {
        try { return GetNodeStrategy(type); }
        catch (MapperNotImplementedException) { }

        try { return GetEdgeStrategy(type); }
        catch (MapperNotImplementedException) { }

        try { return GetGraphStrategy(type); }
        catch (MapperNotImplementedException) { }

        throw new MapperNotImplementedException(type);
    }

    public IGraphStrategy GetGraphStrategy(string type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        return type switch
        {
            string x when x == Utilities.TypeToString<BlockGraph>() => new BlockGraphStrategy(),
            _ => throw new MapperNotImplementedException(type)
        };
    }

    public INodeStrategy GetNodeStrategy(string type)
    {
        return type switch
        {
            string x when x == Utilities.TypeToString<ScriptNode>() => new ScriptNodeStrategy(),
            string x when x == Utilities.TypeToString<TxNode>() => new TxNodeStrategy(),
            _ => throw new MapperNotImplementedException(type)
        };
    }

    public IEdgeStrategy GetEdgeStrategy(string type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        return type switch
        {
            string x when x == Utilities.TypeToString<C2SEdge>() => new C2SEdgeStrategy(),
            string x when x == Utilities.TypeToString<C2TEdge>() => new C2TEdgeStrategy(),
            string x when x == Utilities.TypeToString<S2SEdge>() => new S2SEdgeStrategy(),
            string x when x == Utilities.TypeToString<T2TEdge>() => new T2TEdgeStrategy(),
            _ => throw new MapperNotImplementedException(type)
        };
    }
}
