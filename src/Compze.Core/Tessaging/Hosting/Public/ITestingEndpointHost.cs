using System;
using System.Threading.Tasks;

namespace Compze.Core.Tessaging.Hosting.Public;

public interface ITestingEndpointHost : IEndpointHost
{
    TException AssertThrown<TException>() where TException : Exception;
    Task DisposeAsyncWithoutWaitingForEndpointsToBeAtRest();
}
