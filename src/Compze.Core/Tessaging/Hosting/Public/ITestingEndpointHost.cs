using System.Threading.Tasks;

namespace Compze.Core.Tessaging.Hosting.Public;

public interface ITestingEndpointHost : IEndpointHost
{
    Task DisposeAsyncWithoutWaitingForEndpointsToBeAtRest();
}
