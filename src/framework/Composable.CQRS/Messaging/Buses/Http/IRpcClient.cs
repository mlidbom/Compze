using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;

namespace Composable.Messaging.Buses.Http;

interface IRpcClient
{
   Task<TResult> QueryAsync<TResult>(IRemotableQuery<TResult> query);
   Task<TResult> PostAsync<TResult>(IAtMostOnceCommand<TResult> command);
   Task PostAsync(IAtMostOnceHypermediaCommand command);
}
