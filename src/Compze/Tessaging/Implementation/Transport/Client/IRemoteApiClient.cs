using System.Threading.Tasks;
using Compze.Tessaging.Abstractions;

namespace Compze.Tessaging.Hosting.Implementation.Abstractions.Transport.Client;

interface IRemoteApiClient
{
   Task<TResult> QueryAsync<TResult>(IRemotableQuery<TResult> query);
   Task<TResult> PostAsync<TResult>(IAtMostOnceCommand<TResult> command);
   Task PostAsync(IAtMostOnceHypermediaCommand command);
}
