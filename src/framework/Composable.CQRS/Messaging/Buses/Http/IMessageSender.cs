using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;

namespace Composable.Messaging.Buses.Http;

interface IMessageSender
{
   Task SendAsync(IExactlyOnceEvent @event);
   Task SendAsync(IExactlyOnceCommand command);
}