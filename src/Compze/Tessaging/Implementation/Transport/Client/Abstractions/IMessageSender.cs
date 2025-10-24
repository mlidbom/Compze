using System.Threading.Tasks;
using Compze.Tessaging.Abstractions;

namespace Compze.Tessaging.Implementation.Transport.Client.Abstractions;

interface IRemoteMessageSender
{
   Task SendAsync(IExactlyOnceEvent @event);
   Task SendAsync(IExactlyOnceCommand command);
}