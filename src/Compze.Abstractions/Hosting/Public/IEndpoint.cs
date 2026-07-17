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

   ///<summary>Readiness: completes when this endpoint can reach a handler for every type in<br/>
   /// <paramref name="readinessTypes"/> — the framework-native replacement for the startup readiness probes applications<br/>
   /// used to hand-roll. Awaited on a started endpoint, at a moment of the application's own choosing — typically at<br/>
   /// startup, before opening traffic, front-loading the discovery wait that a waiting send would otherwise make the first<br/>
   /// unlucky caller pay; an orchestrator's readiness probe wires to it, and "not ready within patience → abort startup"<br/>
   /// surfaces a misdeployment once, at boot, instead of as every call timing out forever.</summary>
   ///<remarks>A handler is reachable when the endpoint itself serves the type — in-boundary, needing no discovery — or, per<br/>
   /// the type's kind, when an exactly-once tommand has a bindable receiver (a live handler, or the sole remembered one:<br/>
   /// known-but-down is served by the outbox waiting out the peer's absence) or a request/response type has exactly one live<br/>
   /// route: precisely the availability a send would not have to wait for. What readiness cannot cover — churn during<br/>
   /// operation, hours after it completed — is exactly what waiting sends absorb; the two compose. Waits at most<br/>
   /// <paramref name="patience"/> — null means the endpoint's declared handler-availability patience — then throws<br/>
   /// <see cref="EndpointNotReadyWithinPatienceException"/> naming every type still unavailable and what the endpoint's<br/>
   /// peer memory remembers about it.</remarks>
   Task AwaitReadinessAsync(ReadinessTypes readinessTypes, TimeSpan? patience = null);
}
