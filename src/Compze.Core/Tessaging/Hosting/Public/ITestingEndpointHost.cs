using System;
using System.Threading.Tasks;

namespace Compze.Core.Tessaging.Hosting.Public;

public interface ITestingEndpointHost : IEndpointHost
{
    //Urgent: A client "endpoint" makes no sense. It is just a client, not an endpoint. It should be easy to just get a browser for an API rather than pretending to be an endpoint in order to get one.
    IEndpoint RegisterClientEndpointForRegisteredEndpoints(Action<IEndpointBuilder>? setup = null);
    IClient RegisterClientForRegisteredEndpoints(Action<IEndpointBuilder>? setup = null);
    TException AssertThrown<TException>() where TException : Exception;
    Task DisposeAsyncWithoutWaitingForEndpointsToBeAtRest();
}
