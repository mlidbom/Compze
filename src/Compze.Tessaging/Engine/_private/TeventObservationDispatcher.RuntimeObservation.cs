namespace Compze.Tessaging.Engine._private;

sealed partial class TeventObservationDispatcher
{
   ///<summary>The observers one call to <see cref="TeventObservationDispatcher.ObserveTevents"/> created, which observe until this is<br/>
   /// disposed. Disposing is safe from inside one of its observers' own calls, and disposing twice does nothing more.</summary>
   sealed class RuntimeObservation : IDisposable
   {
      readonly TeventObservationDispatcher _dispatcher;
      readonly IReadOnlyList<ObserverDispatchQueue> _queues;

      internal RuntimeObservation(TeventObservationDispatcher dispatcher, IReadOnlyList<ObserverDispatchQueue> queues)
      {
         _dispatcher = dispatcher;
         _queues = queues;
      }

      public void Dispose() => _dispatcher.EndRuntimeObservation(_queues);
   }
}
