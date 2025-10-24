using System.Threading.Tasks;
using Compze.Tessaging.Abstractions;

namespace Compze.Tessaging.Hosting.Implementation.Http;

interface IRemoteApiClient
{
   Task<TResult> QueryAsync<TResult>(IRemotableQuery<TResult> query);
   Task<TResult> PostAsync<TResult>(IAtMostOnceCommand<TResult> command);
   Task PostAsync(IAtMostOnceHypermediaCommand command);
}
