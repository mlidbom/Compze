using Compze.DependencyInjection.Abstractions;

namespace Compze.Abstractions.Hosting.Public;

///<summary>
/// One deployable unit of a Compze application: its own dependency injection container, the message-handling
/// pipelines wired into it, and the <see cref="IEndpointComponent"/>s that listen and send on its behalf.
///
/// Endpoints are declared against an <see cref="IEndpointHost"/> via
/// <see cref="IEndpointHost.RegisterEndpoint"/>, and configured through an <see cref="IEndpointBuilder"/> —
/// which paradigm pipelines (Tessaging, Typermedia) plug into as features. The endpoint itself is
/// paradigm-blind: what it can do is decided entirely by which features were added while it was built.
///
/// An endpoint's addresses are likewise paradigm-specific and therefore not members of this interface: each
/// paradigm contributes an extension property (such as <c>TessagingAddress</c> and <c>TypermediaAddress</c>)
/// that reads the address from that paradigm's component in <see cref="Components"/>. Such addresses are null
/// until the endpoint is listening, and for endpoints without that paradigm's pipeline.
///</summary>
public interface IEndpoint : IAsyncDisposable
{
   ///<summary>Resolves services from the endpoint's container — the way application code outside a message handler reaches the endpoint's services.</summary>
   IRootResolver ServiceLocator { get; }

   ///<summary>The endpoint's components, materialized when the endpoint starts listening. Empty before that.</summary>
   IReadOnlyList<IEndpointComponent> Components { get; }

   ///<summary>True when both the listening and the sending phase have started; see <see cref="IEndpointComponent"/> for the phase ordering.</summary>
   bool IsRunning { get; }

   Task StartListeningComponentsAsync();
   Task StartSendingComponentsAsync();
   Task StopListeningComponentsAsync();
   Task StopSendingComponentsAsync();
}
