using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Public;

namespace Compze.Tessaging.Implementation.Transport.Client.Abstractions;

interface IRemoteApiClient
{
   Task<TResult> QueryAsync<TResult>(IRemotableQuery<TResult> query);
   Task<TResult> PostAsync<TResult>(IAtMostOnceCommand<TResult> command);
   Task PostAsync(IAtMostOnceHypermediaCommand command);
}
