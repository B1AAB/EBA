namespace BC2G.Exceptions;

internal class ClientInaccessible : Exception
{
    public ClientInaccessible(string message = "") :
        base(message + "Cannot query the Bitcoin client; " +
            "make sure the client is running " +
            "and listening on the provided " +
            "endpoint.")
    { }
}
