namespace Compze.Typermedia.Hosting;

public interface ITypermediaTransportServer : IAsyncDisposable
{
   Uri Address { get; }

   Task StartAsync();
   Task StopAsync();
}
