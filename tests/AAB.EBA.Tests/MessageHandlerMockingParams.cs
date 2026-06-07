using System.Net;
using System.Net.Http;

namespace AAB.EBA.Tests;

internal class MessageHandlerMockingParams(
    string endpoint,
    string response,
    HttpStatusCode status = HttpStatusCode.OK)
{
    public string Endpoint { get; } = endpoint;

    // Implementation details: 
    // it is essential to return a new instance
    // everytime this is accessed, because otherwise
    // any second reader will continue reading 
    // from the point where the first reader left;
    // if left at the end, the second reader will 
    // read empty string.
    public StringContent Response
    {
        get { return new StringContent(_response); }
    }

    public HttpStatusCode Status { get; } = status;

    private readonly string _response = response;
}
