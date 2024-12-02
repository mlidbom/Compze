using System.Threading.Tasks;

namespace Compze.Messaging.Buses.Http;

interface IMessageSender
{
   Task SendAsync(IExactlyOnceEvent @event);
   Task SendAsync(IExactlyOnceCommand command);
}