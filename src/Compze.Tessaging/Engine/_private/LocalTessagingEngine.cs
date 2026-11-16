using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Engine._private;

///<summary>The <see cref="ILocalTessagingEngine"/> of a plain container: its stop is the stop of the engine's only background<br/>
/// work, the <see cref="TeventObservationDispatcher"/>.</summary>
sealed class LocalTessagingEngine : ILocalTessagingEngine
{
   readonly TeventObservationDispatcher _observationDispatcher;

   internal LocalTessagingEngine(TeventObservationDispatcher observationDispatcher) => _observationDispatcher = observationDispatcher;

   public async Task StopAsync() => await _observationDispatcher.StopAsync().caf();

   public async Task AwaitNoTeventsInFlightAsync() => await _observationDispatcher.DrainAsync().caf();
}
