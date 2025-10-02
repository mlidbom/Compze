using System.Threading.Tasks;

namespace Compze.Tessaging.Buses.Http;

interface IRpcClient
{
   Task<TResult> QueryAsync<TResult>(IRemotableQuery<TResult> query);
   Task<TResult> PostAsync<TResult>(IAtMostOnceCommand<TResult> command);
   Task PostAsync(IAtMostOnceHypermediaCommand command);
}
