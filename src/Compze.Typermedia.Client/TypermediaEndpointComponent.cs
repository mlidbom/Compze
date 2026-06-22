using Compze.Abstractions.Hosting.Public;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Typermedia.Hosting;

namespace Compze.Typermedia.Client;

///<summary>The Typermedia pipeline's runtime lifecycle within an endpoint: its transport server listens; nothing participates in the sending phase.</summary>
sealed class TypermediaEndpointComponent(ITypermediaTransportServer transportServer) : IEndpointComponent
{
   readonly ITypermediaTransportServer _transportServer = transportServer;

   bool _isListening;

   ///<summary>The address where the typermedia transport server listens; null until listening.</summary>
   internal EndpointAddress? Address => _isListening ? new EndpointAddress(_transportServer.Address) : null;

   public async Task StartListeningAsync()
   {
      await _transportServer.StartAsync().caf();
      _isListening = true;
      this.Log().Info($"Typermedia listening at {Address}");
   }

   public async Task StopListeningAsync()
   {
      _isListening = false;
      await _transportServer.StopAsync().caf();
   }
}
