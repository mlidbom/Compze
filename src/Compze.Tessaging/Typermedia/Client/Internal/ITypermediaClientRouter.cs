using Compze.Tessaging.Typermedia.Internal;
using Compze.Tessaging.Endpoints.Discovery;

namespace Compze.Tessaging.Typermedia.Client.Internal;

///<summary>An external client application's typermedia router — the static composition a client uses when it knows the<br/>
/// addresses it talks to (e.g. <c>TypermediaTestClient</c>). An endpoint navigating other endpoints' typermedia routes<br/>
/// through its one router instead, which discovers endpoints dynamically.</summary>
interface ITypermediaClientRouter : ITypermediaRouting
{
    ///<summary>Connects to the one endpoint listening at <paramref name="endpointAddress"/> and registers routes for the typermedia<br/>
    /// types it advertises. Call once per known endpoint — a client may navigate several.</summary>
    Task ConnectAsync(EndpointAddress endpointAddress);

    void Start();
    void Stop();
}
