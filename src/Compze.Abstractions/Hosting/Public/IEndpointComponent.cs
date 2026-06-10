namespace Compze.Abstractions.Hosting.Public;

///<summary>
/// A part of an endpoint with a listening/sending lifecycle — typically a paradigm's transport pipeline.
/// The endpoint starts all components' listening before any sending, and stops in the reverse order.
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
