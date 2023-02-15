namespace BC2G.Exceptions;

internal class MapperNotImplementedException : NotImplementedException
{
    public MapperNotImplementedException(string type) :
        base($"A mapper for type {type} is not implemented.")
    { }
}
