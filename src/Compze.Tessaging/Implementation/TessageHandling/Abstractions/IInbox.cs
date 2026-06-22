using Compze.Abstractions.Hosting.Public;
using Compze.Tessaging.Implementation.Transport.Abstractions;

namespace Compze.Tessaging.Implementation.TessageHandling.Abstractions;

public interface IInbox
{
   EndpointAddress Address { get; }
   Task StartAsync();
   Task StopAsync();

   Task ReceiveAsync(TransportTessage.InComing tessage);
}
