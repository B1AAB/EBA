namespace BC2G.Exceptions;

internal class MapperNotImplementedException : NotImplementedException
{
    public MapperNotImplementedException(GraphComponentType type) :
        base($"A mapper for type {type} is not implemented.")
    { }
}
