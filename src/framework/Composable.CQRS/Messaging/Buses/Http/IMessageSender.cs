using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Serialization;

namespace Composable.Messaging.Buses.Http;

interface IMessageSender
{
   Task SendAsync(EndPointAddress address, TransportMessage.OutGoing message, IExactlyOnceEvent @event, IRemotableMessageSerializer serializer);
   Task SendAsync(EndPointAddress address, TransportMessage.OutGoing message, IExactlyOnceCommand command, IRemotableMessageSerializer serializer);
}