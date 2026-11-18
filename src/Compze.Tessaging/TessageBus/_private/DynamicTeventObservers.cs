using Compze.Tessaging.Engine._private;

namespace Compze.Tessaging.TessageBus._private;

///<summary>The <see cref="IDynamicTeventObservers"/> of an engine: its dynamic observers are queued by the engine's<br/>
/// <see cref="TeventObservationDispatcher"/>, beside the composed observers.</summary>
sealed class DynamicTeventObservers : IDynamicTeventObservers
{
   readonly TeventObservationDispatcher _dispatcher;

   internal DynamicTeventObservers(TeventObservationDispatcher dispatcher) => _dispatcher = dispatcher;

   public IDisposable ObserveTevents(Action<TeventObservationRegistrar> observe) => _dispatcher.ObserveTevents(observe);
}
