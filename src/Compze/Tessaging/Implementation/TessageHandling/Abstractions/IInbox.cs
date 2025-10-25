using System.Threading.Tasks;
using Compze.Core.Tessaging.Transport.Internal;

namespace Compze.Tessaging.Implementation.TessageHandling.Abstractions;

interface IInbox
{
   HttpEndPointAddress Address { get; }
   Task StartAsync();
   Task StopAsync();

   Task<object?> Receive(TransportTessage.InComing tessage);
}