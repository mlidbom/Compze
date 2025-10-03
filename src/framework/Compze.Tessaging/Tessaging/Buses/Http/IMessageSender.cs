using System.Threading.Tasks;

namespace Compze.Tessaging.Buses.Http;

interface IMessageSender
{
   Task SendAsync(IExactlyOnceEvent @event);
   Task SendAsync(IExactlyOnceCommand command);
}