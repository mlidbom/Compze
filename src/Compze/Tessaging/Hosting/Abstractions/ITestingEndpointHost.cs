using System;

namespace Compze.Tessaging.Hosting.Abstractions;

public interface ITestingEndpointHost : IEndpointHost
{
    IEndpoint RegisterTestingEndpoint(string? name = null, EndpointId? id = null, Action<IEndpointBuilder>? setup = null);

    //Urgent: A client "endpoint" makes no sense. It is just a client, not an endpoint. It should be easy to just get a browser for an API rather than pretending to be an endpoint in order to get one.
    IEndpoint RegisterClientEndpointForRegisteredEndpoints();
    TException AssertThrown<TException>() where TException : Exception;
    bool WaitForEndPointsToBeAtRestOnDispose { get; set; }
}
