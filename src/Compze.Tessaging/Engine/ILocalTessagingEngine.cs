using Compze.Tessaging.Engine.Wiring;

namespace Compze.Tessaging.Engine;

///<summary>The LocalTessagingEngine of a plain container, composed by <see cref="LocalTessagingEngineRegistrar.LocalTessagingEngine"/>:<br/>
/// the object its lifecycle is driven through. Resolve it from the container to stop the engine before disposing the container.</summary>
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
}
