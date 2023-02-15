using BC2G.Blockchains.Bitcoin.Graph;
using BC2G.Exceptions;

namespace BC2G.Graph.Db.Neo4j.BitcoinMappers;

public class MapperFactory : IMapperFactory
{
    public IMapperBase GetMapperBase(string type)
    {
        try { return GetEdgeMapper(type); }
        catch (MapperNotImplementedException) { }

        try { return GetGraphMapper(type); }
        catch (MapperNotImplementedException) { }

        throw new MapperNotImplementedException(type);
    }

    public IEdgeMapper GetEdgeMapper(string type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        return type switch
        {
            string x when x == Utilities.TypeToString<S2SEdge>() => new S2SEdgeMapper(),
            string x when x == Utilities.TypeToString<T2TEdge>() => new T2TEdgeMapper(),
            string x when x == Utilities.TypeToString<C2SEdge>() => new C2SEdgeMapper(),
            string x when x == Utilities.TypeToString<C2TEdge>() => new C2TEdgeMapper(),
            _ => throw new MapperNotImplementedException(type)
        };
    }

    public IGraphMapper GetGraphMapper(string type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        return type switch
        {
            string x when x == Utilities.TypeToString<BitcoinBlockGraph>() => new BitcoinBlockGraphMapper(),
            _ => throw new MapperNotImplementedException(type)
        };
    }
}
