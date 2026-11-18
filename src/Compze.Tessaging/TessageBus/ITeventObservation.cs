using Compze.Tessaging.Engine;

namespace Compze.Tessaging.TessageBus;

///<summary>Creates dynamic observers: tevent observers that come and go while the application runs, such as one for a view that<br/>
/// shows what the tevents change. Each observes until what creating it returned is disposed. Resolved from the container, in a<br/>
/// plain container's composition and in an endpoint's alike.</summary>
///<remarks>A dynamic observer is declared through the same <see cref="TeventObservationRegistrar"/> as a composed observer, one<br/>
/// declared at composition with <see cref="LocalTessagingEngineBuilder.ObserveTevents"/>, which lives as long as the container. It<br/>
/// observes in the same way: committed tevents only, off the publishing thread, in commit order in a queue of its own, each call<br/>
/// in a fresh scope with no transaction. A throwing observer is reported to the background-exception reporter, and its queue<br/>
/// goes on. <see cref="ILocalTessagingEngine.AwaitNoTessagesInFlightAsync"/> and <see cref="ILocalTessagingEngine.StopAsync"/><br/>
/// wait for it too.<br/>
/// There is no dynamic participation: a participant added while the application runs would run inside every publisher's<br/>
/// transaction, and its failure would roll back the publisher's change.</remarks>
///<remarks>In a plain container, a dynamic observer may observe any tevent type: there are no other endpoints. In an endpoint, the<br/>
/// endpoint subscribes, where it is composed, to what its dynamic observers may observe, with<br/>
/// <see cref="Endpoints.EndpointBuilder.SubscribeForDynamicObservers{TTevent}"/>: a dynamic observer's types must each be<br/>
/// compatible with a type subscribed to for them, even for tevents only the endpoint's own process publishes. The endpoint asks<br/>
/// its peers for those types from its start, so a dynamic observer observes such tevents committed in its own process and those<br/>
/// that arrive from other endpoints, from the moment <see cref="ObserveTevents"/> returns, with no round trip.<br/>
/// review-naming: ITeventObservation is a placeholder name, for the naming pass (jastudio-rep-opus, 2026-11-17).</remarks>
public interface ITeventObservation
{
   ///<summary>Creates the dynamic observers <paramref name="observe"/> declares. They observe every tevent queued for observation<br/>
   /// after this returns; a unit of work that commits while it runs may or may not be observed.<br/>
   /// Throws once the engine has stopped, since nothing would be observed, and, in an endpoint, for an observer of a tevent type<br/>
   /// the endpoint didn't subscribe to with <see cref="Endpoints.EndpointBuilder.SubscribeForDynamicObservers{TTevent}"/>.</summary>
   ///<returns>What ends the observers when disposed: nothing more is queued for them, and what is queued and not yet started is<br/>
   /// dropped. Disposing doesn't wait for a call already running, so a view disposed on its UI thread can't deadlock with an<br/>
   /// observer waiting for that thread; such a call may still finish after disposing returns. Disposing is safe from inside the<br/>
   /// observer's own call, and disposing twice does nothing more.</returns>
   IDisposable ObserveTevents(Action<TeventObservationRegistrar> observe);
}
