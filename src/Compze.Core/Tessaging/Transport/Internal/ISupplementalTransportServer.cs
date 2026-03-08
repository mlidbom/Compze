namespace Compze.Core.Tessaging.Transport.Internal;

/// <summary>A transport server that binds to the same address as the primary inbox transport server.
/// Used by paradigms (Typermedia, Infrastructure) to register their own in-memory network bindings
/// without coupling the inbox server to their implementations.</summary>
public interface ISupplementalTransportServer
{
   Task StartAsync(EndPointAddress address);
   Task StopAsync();
}
