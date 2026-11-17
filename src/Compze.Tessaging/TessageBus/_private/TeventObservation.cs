using Compze.Tessaging.Engine._private;

namespace Compze.Tessaging.TessageBus._private;

///<summary>The <see cref="ITeventObservation"/> of an engine: its runtime observers are queued by the engine's<br/>
/// <see cref="TeventObservationDispatcher"/>, beside the observers declared at composition.</summary>
sealed class TeventObservation : ITeventObservation
{
   readonly TeventObservationDispatcher _dispatcher;

   internal TeventObservation(TeventObservationDispatcher dispatcher) => _dispatcher = dispatcher;

   public IDisposable ObserveTevents(Action<TeventObservationRegistrar> observe) => _dispatcher.ObserveTevents(observe);
}
