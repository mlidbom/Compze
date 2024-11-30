using System.Threading.Tasks;

namespace Composable.Messaging.Buses.Http;

interface IMessageSender
{
   Task SendAsync(IExactlyOnceEvent @event);
   Task SendAsync(IExactlyOnceCommand command);
}