using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;

namespace Compze.Tessaging.Implementation.Transport.Client.Internal;

interface IInfrastructureQueryTransport
{
   Task<TResult> GetAsync<TResult>(IQuery<TResult> query, EndPointAddress address);
}
