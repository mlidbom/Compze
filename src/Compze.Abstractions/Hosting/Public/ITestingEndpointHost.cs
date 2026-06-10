namespace Compze.Abstractions.Hosting.Public;

public interface ITestingEndpointHost : IEndpointHost
{
    Task DisposeAsyncWithoutWaitingForEndpointsToBeAtRest();
}
