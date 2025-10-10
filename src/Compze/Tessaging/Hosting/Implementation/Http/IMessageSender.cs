using System.Threading.Tasks;
using Compze.Tessaging.Abstractions;

namespace Compze.Tessaging.Hosting.Implementation.Http;

interface IMessageSender
{
   Task SendAsync(IExactlyOnceEvent @event);
   Task SendAsync(IExactlyOnceCommand command);
}