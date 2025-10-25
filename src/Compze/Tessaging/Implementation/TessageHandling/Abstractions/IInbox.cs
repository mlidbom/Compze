using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Transport.Internal;

namespace Compze.Tessaging.Implementation.TessageHandling.Abstractions;

interface IInbox
{
   HttpEndPointAddress Address { get; }
   Task StartAsync();
   Task StopAsync();

   Task<object?> Receive(TransportTessage.InComing tessage);
}