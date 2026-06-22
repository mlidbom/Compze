using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Hosting.Public;

namespace Compze.Internals.Transport;

public interface IInfrastructureQueryTransport
{
   Task<TResult> GetAsync<TResult>(IQuery<TResult> query, EndpointAddress address);
}
