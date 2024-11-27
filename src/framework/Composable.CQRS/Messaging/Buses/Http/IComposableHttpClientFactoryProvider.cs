using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Serialization;

namespace Composable.Messaging.Buses.Http;

interface IMessageSender
{
   Task SendAsync(EndPointAddress address, TransportMessage.OutGoing message, IExactlyOnceEvent @event, IRemotableMessageSerializer serializer);
   Task SendAsync(EndPointAddress address, TransportMessage.OutGoing message, IExactlyOnceCommand command, IRemotableMessageSerializer serializer);
}

interface IRpcClient
{
   Task<TResult> QueryAsync<TResult>(EndPointAddress address, TransportMessage.OutGoing message, IRemotableQuery<TResult> query, IRemotableMessageSerializer serializer);
   Task<TResult> PostAsync<TResult>(EndPointAddress address, TransportMessage.OutGoing message, IAtMostOnceCommand<TResult> query, IRemotableMessageSerializer serializer);
   Task PostAsync(EndPointAddress address, TransportMessage.OutGoing message, IAtMostOnceHypermediaCommand query, IRemotableMessageSerializer serializer);
}
