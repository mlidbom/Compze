using System.Collections.Concurrent;
using System.Diagnostics;
using Compze.Internals.SystemCE._private;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Diagnostics;

namespace Compze.Sql.MicrosoftSql._internal;

///<summary>PROBE, for the flake hunt only: reports two operations at once on one SQL Server physical connection, identified by<br/>
/// its ClientConnectionId, and a connection closed while an operation on it is in progress. It listens to SqlClient's diagnostic<br/>
/// events, and Compze's reading of a reader's rows adds a span of its own.</summary>
static class ProbeSqlClientConcurrency
{
   static int _subscribed;

   sealed class Operation(string kind, SqlConnection? connection, string? commandText)
   {
      public readonly string Kind = kind;
      public readonly int ConnectionObject = connection == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(connection);
      public readonly string CommandText = commandText == null ? "" : commandText.Length > 400 ? commandText[..400] : commandText;
      public readonly int Thread = Environment.CurrentManagedThreadId;
      public readonly string Stack = Environment.StackTrace;
      public readonly long Started = Stopwatch.GetTimestamp();

      public override string ToString() => $"""
                                             {Kind} on SqlConnection object {ConnectionObject}, thread {Thread}, {Stopwatch.GetElapsedTime(Started).TotalMilliseconds:F1} ms ago
                                             SQL: {CommandText}
                                             Stack:
                                             {Stack}
                                             """;
   }

   static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Operation>> InFlightByPhysicalConnection = new();
   static readonly ConcurrentDictionary<Guid, Guid> PhysicalConnectionOfOperation = new();

   internal static void EnsureSubscribed()
   {
      if(Interlocked.Exchange(ref _subscribed, 1) == 0) DiagnosticListener.AllListeners.Subscribe(new AllListenersObserver());
   }

   ///<summary>A span of Compze's own around reading a reader's rows, which SqlClient's events don't cover.</summary>
   internal static IDisposable Track(SqlCommand command, string kind)
   {
      var operationId = Guid.NewGuid();
      Before(operationId, command.Connection?.ClientConnectionId, new Operation(kind, command.Connection, command.CommandText));
      return new Ender(operationId);
   }

   sealed class Ender(Guid operationId) : IDisposable
   {
      public void Dispose() => After(operationId);
   }

   static void Before(Guid operationId, Guid? physicalConnection, Operation operation)
   {
      if(physicalConnection is not {} id || id == Guid.Empty) return;
      var inFlight = InFlightByPhysicalConnection.GetOrAdd(id, _ => new ConcurrentDictionary<Guid, Operation>());
      PhysicalConnectionOfOperation[operationId] = id;
      inFlight[operationId] = operation; //Added before looking, so that two operations starting at once both see each other.
      foreach(var (otherId, other) in inFlight)
      {
         if(otherId == operationId) continue;
         //A reader's rows are read on the thread and connection that executed it: that nesting is the normal case.
         if(other.Thread == operation.Thread && other.ConnectionObject == operation.ConnectionObject) continue;
         Report($"two operations at once on physical connection {id}", operation, other);
      }
   }

   static void After(Guid operationId)
   {
      if(PhysicalConnectionOfOperation.TryRemove(operationId, out var id) && InFlightByPhysicalConnection.TryGetValue(id, out var inFlight))
         inFlight.TryRemove(operationId, out _);
   }

   static void CloseBefore(Guid? physicalConnection, SqlConnection? connection)
   {
      if(physicalConnection is not {} id || id == Guid.Empty) return;
      if(!InFlightByPhysicalConnection.TryGetValue(id, out var inFlight)) return;
      var closing = new Operation("Close", connection, null);
      foreach(var other in inFlight.Values) Report($"closing physical connection {id} while an operation on it is in progress", closing, other);
   }

   static void Report(string what, Operation @this, Operation other) =>
      ProbeLog.Write($"""
                      PROBE SqlClient: {what}
                      ==== This one:
                      {@this}
                      ==== The other, in progress:
                      {other}
                      """);

   sealed class AllListenersObserver : IObserver<DiagnosticListener>
   {
      public void OnNext(DiagnosticListener listener)
      {
         if(listener.Name == "SqlClientDiagnosticListener") listener.Subscribe(new SqlClientObserver());
      }

      public void OnCompleted() {}
      public void OnError(Exception error) {}
   }

   sealed class SqlClientObserver : IObserver<KeyValuePair<string, object?>>
   {
      static int _armed;

      public void OnNext(KeyValuePair<string, object?> value)
      {
         if(Interlocked.Exchange(ref _armed, 1) == 0) Console.Error.WriteLine($"PROBE SqlClient armed: the first event is {value.Key}, {value.Value?.GetType().Name}");
         switch(value.Value)
         {
            case SqlClientCommandBefore before:
               Before(before.OperationId, before.ConnectionId, new Operation(before.Operation, before.Command.Connection, before.Command.CommandText));
               break;
            case SqlClientCommandAfter after:
               After(after.OperationId);
               break;
            case SqlClientCommandError error:
               After(error.OperationId);
               break;
            case SqlClientConnectionCloseBefore closeBefore:
               CloseBefore(closeBefore.ConnectionId, closeBefore.Connection);
               break;
         }
      }

      public void OnCompleted() {}
      public void OnError(Exception error) {}
   }
}
