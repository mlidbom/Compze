using Compze.DependencyInjection.Abstractions;

namespace Compze.Abstractions.Hosting.Public;

public interface IEndpoint : IAsyncDisposable
{
   IRootResolver ServiceLocator { get; }

   ///<summary>The endpoint's components, materialized when the endpoint starts listening. Empty before that.</summary>
   IReadOnlyList<IEndpointComponent> Components { get; }

   bool IsRunning { get; }
   Task StartListeningComponentsAsync();
   Task StartSendingComponentsAsync();
   Task StopListeningComponentsAsync();
   Task StopSendingComponentsAsync();
}
