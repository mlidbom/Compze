using Compze.Tessaging.Engine;

namespace Compze.Tessaging.TessageBus;

///<summary>Creates tevent observers while the application runs, such as for a view that shows what the tevents change: each<br/>
/// observes until what creating it returned is disposed. Resolved from the container, in a plain container's composition and<br/>
/// in an endpoint's alike.</summary>
///<remarks>A runtime observer is declared through the same <see cref="TeventObservationRegistrar"/> as one declared at composition<br/>
/// with <see cref="LocalTessagingEngineBuilder.ObserveTevents"/>, and observes in the same way: committed tevents only, off the<br/>
/// publishing thread, in commit order in a queue of its own, each call in a fresh scope with no transaction. A throwing observer<br/>
/// is reported to the background-exception reporter, and its queue goes on. <see cref="ILocalTessagingEngine.AwaitNoTessagesInFlightAsync"/><br/>
/// and <see cref="ILocalTessagingEngine.StopAsync"/> wait for it too.<br/>
/// There is no participation at runtime: a participant added at runtime would run inside every publisher's transaction, and its<br/>
/// failure would roll back the publisher's change.</remarks>
///<remarks>In an endpoint, a runtime observer observes the tevents committed in its own process, and the remote ones the endpoint<br/>
/// receives anyway: an endpoint receives a remote tevent only of the types it declared handlers or observers for at composition,<br/>
/// since it tells its peers what to send it.<br/>
/// review-naming: ITeventObservation is a placeholder name, for the naming pass (jastudio-rep-opus, 2026-11-17).</remarks>
public interface ITeventObservation
{
   ///<summary>Creates the observers <paramref name="observe"/> declares. They observe every tevent queued for observation after<br/>
   /// this returns; a unit of work that commits while it runs may or may not be observed.<br/>
   /// Throws once the engine has stopped, since nothing would be observed.</summary>
   ///<returns>What ends the observers when disposed: nothing more is queued for them, and what is queued and not yet started is<br/>
   /// dropped. Disposing doesn't wait for a call already running, so a view disposed on its UI thread can't deadlock with an<br/>
   /// observer waiting for that thread; such a call may still finish after disposing returns. Disposing is safe from inside the<br/>
   /// observer's own call, and disposing twice does nothing more.</returns>
   IDisposable ObserveTevents(Action<TeventObservationRegistrar> observe);
}
