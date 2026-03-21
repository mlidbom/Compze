using Compze.Core.Tessaging.Transport.Internal;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Core.Tessaging.Hosting.Public;

public interface IEndpoint : IAsyncDisposable
{
   IRootResolver ServiceLocator { get; }
   EndPointAddress? Address { get; }
   EndPointAddress? TypermediaAddress { get; }
   bool IsRunning { get; }
   Task StartListeningComponentsAsync();
   Task StartSendingComponentsAsync();
   Task StopListeningComponentsAsync();
   Task StopSendingComponentsAsync();
}
