namespace Compze.Abstractions.Hosting.Public;

///<summary>
/// A part of an endpoint with a listening/announcing/sending lifecycle — typically a transport pipeline such as
/// Tessaging's or Typermedia's, added while the endpoint is built via <see cref="IEndpointBuilder.AddComponent"/>.
///
/// The phased lifecycle exists for one reason: an <see cref="IEndpointHost"/> runs each phase host-wide —
/// every component's listening phase completes before any component's announcing phase, and every announcement
/// before any component's sending phase. So nothing can send to an endpoint that is not yet ready to receive,
/// an announced address is always one whose whole endpoint is already listening, and a sending-phase component
/// that reads announcements — a router taking its first look at an endpoint registry — sees every endpoint the
/// host announced, not a partial membership that happens to have been announced first. Stopping runs in
/// reverse: addresses are retracted before any sending stops, and sending stops before listening, so an
/// address stops being advertised before anything goes deaf.
///
/// Only the phases every component has are abstract; the rest are default-implemented as no-ops — most
/// components neither announce an address nor initiate sending.
///
/// A component that owns resources should also implement <see cref="IAsyncDisposable"/>; the endpoint disposes
/// such components after its container has been disposed.
///</summary>
public interface IEndpointComponent
{
   Task StartListeningAsync();
   Task AnnounceAddressAsync() => Task.CompletedTask;
   Task StartSendingAsync() => Task.CompletedTask;
   Task StopSendingAsync() => Task.CompletedTask;
   Task RetractAddressAsync() => Task.CompletedTask;
   Task StopListeningAsync();
}
