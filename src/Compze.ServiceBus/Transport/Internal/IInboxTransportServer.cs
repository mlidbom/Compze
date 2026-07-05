namespace Compze.ServiceBus.Transport.Internal;

public interface IInboxTransportServer : IAsyncDisposable
{
   /// <summary>The network address where the inbox is listening (e.g., "http://127.0.0.1:5000", "memory://an-endpoint-id")</summary>
   Uri Address { get; }

   Task StartAsync();

   Task StopAsync();
}
