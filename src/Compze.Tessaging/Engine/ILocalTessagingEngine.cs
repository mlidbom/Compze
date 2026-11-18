using Compze.Tessaging.Engine.Wiring;

namespace Compze.Tessaging.Engine;

///<summary>The LocalTessagingEngine of a plain container, composed by <see cref="LocalTessagingEngineRegistrar.LocalTessagingEngine"/>:<br/>
/// the object its lifecycle is driven through. Resolve it from the container to stop the engine before disposing the container,<br/>
/// or to wait for its observers.</summary>
///<remarks>The engine's one piece of background work is dispatching committed tevents to their observers, off the publishing<br/>
/// thread. Unless the engine is stopped first, the container's disposal finishes that work: disposal then does a stop's job,<br/>
/// waits for the observers, and, disposed asynchronously, may go on disposing the rest of the container on whichever thread the<br/>
/// last observer finished on. <see cref="StopAsync"/> does that job before disposal, so disposal only releases.<br/>
/// An endpoint composition has no <see cref="ILocalTessagingEngine"/>: an endpoint's own stop stops its engine.</remarks>
public interface ILocalTessagingEngine
{
   ///<summary>Stops the engine: waits, holding no thread, until every tevent committed so far has been observed by every<br/>
   /// observer that observes it, including what the observers themselves publish meanwhile. From then on the engine refuses<br/>
   /// to publish tevents, since no observer would observe them. Disposes nothing: dispose the container afterwards as usual.<br/>
   /// Stopping a stopped engine does nothing more.</summary>
   Task StopAsync();

   ///<summary>Waits, holding no thread, until no tessage is in flight in the engine. Leaves the engine running: such as for<br/>
   /// a specification that asserts on what an observer did, and goes on, or for a batch step or a tool of an application that waits<br/>
   /// until the observers have caught up before it reports, exports or backs up.</summary>
   ///<remarks>The tessages in flight in this engine are the committed tevents its observers have yet to observe, so it waits until every<br/>
   /// tevent committed so far has been observed by every observer that observes it, including what the observers themselves publish<br/>
   /// meanwhile.<br/>
   /// <b>No tessage is in flight only while nothing else publishes.</b> A tevent committed during the wait, by code other than the<br/>
   /// observers, may or may not be waited for, and one committed after it returns is in flight again. So it is for code that controls<br/>
   /// what runs, such as a specification or a batch step that has finished its own changes.<br/>
   /// It waits at most 30 seconds for each observer's queue, and throws <see cref="Compze.Threading.Exceptions.AwaitingConditionTimeoutException"/><br/>
   /// when one hasn't emptied by then, such as behind an observer that hangs.</remarks>
   Task AwaitNoTessagesInFlightAsync();
}
