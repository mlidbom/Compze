using System.Threading.Tasks;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Transport.Abstractions;

namespace Compze.Tessaging.Implementation.TessageHandling.Abstractions;

interface IInbox
{
   EndPointAddress Address { get; }
   Task StartAsync();
   Task StopAsync();

   Task<object?> Receive(TransportTessage.InComing tessage);
}