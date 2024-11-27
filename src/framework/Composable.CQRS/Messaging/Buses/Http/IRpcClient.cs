using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Serialization;

namespace Composable.Messaging.Buses.Http;

interface IRpcClient
{
   Task<TResult> QueryAsync<TResult>(EndPointAddress address, TransportMessage.OutGoing message, IRemotableQuery<TResult> query, IRemotableMessageSerializer serializer);
   Task<TResult> PostAsync<TResult>(EndPointAddress address, TransportMessage.OutGoing message, IAtMostOnceCommand<TResult> query, IRemotableMessageSerializer serializer);
   Task PostAsync(EndPointAddress address, TransportMessage.OutGoing message, IAtMostOnceHypermediaCommand query, IRemotableMessageSerializer serializer);
}
