using Compze.Abstractions.Hosting.Public;
using Compze.ServiceBus.Implementation.Transport.Abstractions;

namespace Compze.ServiceBus.Implementation.TessageHandling.Abstractions;

public interface IInbox
{
   EndpointAddress Address { get; }
   Task StartAsync();
   Task StopAsync();

   Task ReceiveAsync(TransportTessage.InComing tessage);
}
