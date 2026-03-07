using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Transport.Abstractions;

namespace Compze.Tessaging.Implementation.TessageHandling.Abstractions;

public interface IInbox
{
   EndPointAddress Address { get; }
   Task StartAsync();
   Task StopAsync();

   Task ReceiveAsync(TransportTessage.InComing tessage);
}