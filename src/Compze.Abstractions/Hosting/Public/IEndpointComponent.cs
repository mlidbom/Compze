namespace Compze.Abstractions.Hosting.Public;

///<summary>
/// A part of an endpoint with a listening/sending lifecycle — typically a paradigm's transport pipeline,
/// added while the endpoint is built via <see cref="IEndpointBuilder.AddComponent"/>.
///
/// The two-phase lifecycle exists for one reason: an <see cref="IEndpointHost"/> starts every component's
/// listening phase host-wide before any component's sending phase, so nothing can send to an endpoint that is
/// not yet ready to receive. Stopping runs in reverse: sending stops before listening. The sending phases are
/// default-implemented as no-ops for components that only listen — Typermedia's transport server, for example,
/// serves requests but never initiates sending, while Tessaging's component runs its outbox in the sending
/// phase.
///
/// A component that owns resources should also implement <see cref="IAsyncDisposable"/>; the endpoint disposes
/// such components after its container has been disposed.
///</summary>
public interface IEndpointComponent
{
   Task StartListeningAsync();
   Task StartSendingAsync() => Task.CompletedTask;
   Task StopSendingAsync() => Task.CompletedTask;
   Task StopListeningAsync();
}
