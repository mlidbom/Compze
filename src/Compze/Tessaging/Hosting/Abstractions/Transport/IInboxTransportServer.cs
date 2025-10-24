using System;
using System.Threading.Tasks;

namespace Compze.Tessaging.Hosting.Abstractions.Transport;

public interface IInboxTransportServer : IAsyncDisposable
{
   /// <summary>The network address where the inbox is listening (e.g., "http://127.0.0.1:5000")</summary>
   string Address { get; }

   Task StartAsync();

   Task StopAsync();
}
