using Compze.Abstractions.Hosting.Public;

namespace Compze.Typermedia.Client;

public interface ITypermediaRouter : ITypermediaRouting
{
    ///<summary>Connects to the one endpoint listening at <paramref name="endpointAddress"/> and registers routes for the typermedia<br/>
    /// types it handles — the static composition an external client uses when it knows the address it talks to<br/>
    /// (e.g. <c>TypermediaTestClient</c>). An endpoint discovering other endpoints dynamically uses<br/>
    /// <see cref="StartMaintainingConnectionsAsync"/> instead.</summary>
    Task ConnectAsync(EndpointAddress endpointAddress);

    ///<summary>Connects to every endpoint the registry currently lists and keeps reconciling the connections with the registry's<br/>
    /// membership until the router stops: an endpoint that appears is connected and its typermedia routes registered, one whose<br/>
    /// address disappears is disconnected and its routes dropped, and one that reappears at a new address — addresses are<br/>
    /// per-instance, identity is the <see cref="EndpointId"/> — has its connection replaced. The first reconciliation completes<br/>
    /// before this method returns, so startup sees the registry's current membership connected.</summary>
    Task StartMaintainingConnectionsAsync(IEndpointRegistry endpointRegistry);

    void Start();
    void Stop();
}
