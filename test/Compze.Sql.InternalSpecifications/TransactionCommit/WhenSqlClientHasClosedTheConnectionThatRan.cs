using System.Diagnostics;
using Microsoft.Data.SqlClient.Diagnostics;

namespace Compze.Sql.InternalSpecifications.TransactionCommit;

///<summary>Runs an action at the moment SqlClient has closed the connection that ran a command whose text holds a marker: inside the<br/>
/// close, once the physical connection is back in SqlClient's pool, and before the close returns to the code that closed it. It<br/>
/// listens to SqlClient's diagnostic events, which SqlClient raises on the thread doing the work.</summary>
sealed class WhenSqlClientHasClosedTheConnectionThatRan : IDisposable, IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>
{
   readonly string _commandTextMarker;
   readonly Action _action;
   readonly IDisposable _listenersSubscription;
   readonly List<IDisposable> _eventSubscriptions = [];
   Guid? _physicalConnection;
   int _hasRun;

   internal WhenSqlClientHasClosedTheConnectionThatRan(string commandTextMarker, Action action)
   {
      _commandTextMarker = commandTextMarker;
      _action = action;
      _listenersSubscription = DiagnosticListener.AllListeners.Subscribe(this);
   }

   void IObserver<DiagnosticListener>.OnNext(DiagnosticListener listener)
   {
      if(listener.Name == "SqlClientDiagnosticListener") _eventSubscriptions.Add(listener.Subscribe(this));
   }

   void IObserver<KeyValuePair<string, object?>>.OnNext(KeyValuePair<string, object?> diagnosticEvent)
   {
      switch(diagnosticEvent.Value)
      {
         case SqlClientCommandBefore before when before.Command.CommandText.Contains(_commandTextMarker, StringComparison.Ordinal):
            _physicalConnection = before.ConnectionId;
            break;
         case SqlClientConnectionCloseAfter closed when closed.ConnectionId == _physicalConnection && Interlocked.Exchange(ref _hasRun, 1) == 0:
            _action();
            break;
      }
   }

   public void Dispose()
   {
      _listenersSubscription.Dispose();
      _eventSubscriptions.ForEach(it => it.Dispose());
   }

   void IObserver<DiagnosticListener>.OnCompleted() {}
   void IObserver<DiagnosticListener>.OnError(Exception error) {}
   void IObserver<KeyValuePair<string, object?>>.OnCompleted() {}
   void IObserver<KeyValuePair<string, object?>>.OnError(Exception error) {}
}
