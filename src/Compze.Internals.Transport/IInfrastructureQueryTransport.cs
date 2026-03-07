using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;

namespace Compze.Internals.Transport;

public interface IInfrastructureQueryTransport
{
   Task<TResult> GetAsync<TResult>(IQuery<TResult> query, EndPointAddress address);
}
