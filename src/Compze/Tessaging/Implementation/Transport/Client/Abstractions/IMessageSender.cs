using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Public;

namespace Compze.Tessaging.Implementation.Transport.Client.Abstractions;

interface IRemoteMessageSender
{
   Task SendAsync(IExactlyOnceEvent @event);
   Task SendAsync(IExactlyOnceCommand command);
}