using Compze.DependencyInjection.Abstractions;

namespace Compze.Abstractions.Hosting.Public;

///<summary>
/// One deployable unit of a Compze application: its own dependency injection container and the machinery that listens and
/// sends on its behalf. An endpoint is a plain composition root — what it is, is decided entirely by its composition
/// (e.g. <c>ExactlyOnceEndpoint.Compose</c> / <c>BestEffortEndpoint.Compose</c> in Compze.Tessaging) — and it drives its own
/// lifecycle phases in the methods below: listen → announce → send on the way up, retract → stop sending → stop listening on
/// the way down. An <see cref="IEndpointHost"/> owns a set of endpoints and runs each phase host-wide
/// (<see cref="IEndpointHost.RegisterEndpoint{TEndpoint}"/>).
///</summary>
public interface IEndpoint : IAsyncDisposable
{
   ///<summary>Resolves services from the endpoint's container — the way application code outside a message handler reaches the endpoint's services.</summary>
   IRootResolver ServiceLocator { get; }

   ///<summary>True when both the listening and the sending phase have started.</summary>
   bool IsRunning { get; }

   Task StartListeningAsync();
   Task AnnounceAddressAsync();
   Task StartSendingAsync();
   Task StopSendingAsync();
   Task RetractAddressAsync();
   Task StopListeningAsync();
}
