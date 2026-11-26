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
      //SqlClient completes an async command's task before it writes the command's After event, and the awaiting code may close
      //the connection or start its next command before the event. So an async operation also ends when the Compze code that
      //awaited it returns: see OperationsReturned.
      public bool IsAsync => Kind.EndsWith("Async", StringComparison.Ordinal);

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
      if(Interlocked.Exchange(ref _subscribed, 1) == 0)
      {
         DiagnosticListener.AllListeners.Subscribe(new AllListenersObserver());
         Compze.Sql.Common._internal.DbConnectionPool<ICompzeMsSqlConnection, SqlCommand>.ProbeOperationsReturned =
            connection => OperationsReturned(((ICompzeMsSqlConnection.CompzeMsSqlConnection)connection).Connection);
      }
   }

   ///<summary>Compze's code that used <paramref name="connection"/> has returned, so every async command it awaited there has<br/>
   /// completed, whenever SqlClient writes their After events: they are done, and what overlapped them came after.</summary>
   static readonly ConcurrentDictionary<int, (long Last, int Calls, int Removed)> HookCallsByConnectionObject = new();

   static void OperationsReturned(SqlConnection connection)
   {
      var connectionObject = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(connection);
      var removed = 0;
      try
      {
         RemoveTheAsyncOperationsOf(connectionObject, ref removed);
      }
      finally
      {
         HookCallsByConnectionObject.AddOrUpdate(connectionObject, (Stopwatch.GetTimestamp(), 1, removed), (_, it) => (Stopwatch.GetTimestamp(), it.Calls + 1, it.Removed + removed));
      }
   }

   static void RemoveTheAsyncOperationsOf(int connectionObject, ref int removed)
   {
      foreach(var (operationId, physicalConnection) in PhysicalConnectionOfOperation)
      {
         if(!InFlightByPhysicalConnection.TryGetValue(physicalConnection, out var inFlight)) continue;
         if(inFlight.TryGetValue(operationId, out var operation) && operation.IsAsync && operation.ConnectionObject == connectionObject)
         {
            inFlight.TryRemove(operationId, out _);
            PhysicalConnectionOfOperation.TryRemove(operationId, out _);
            removed++;
         }
      }
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
         ProbeLog.Write(ReportText($"two operations at once on physical connection {id}", operation, other));
      }
   }

   static void After(Guid operationId)
   {
      if(!PhysicalConnectionOfOperation.TryRemove(operationId, out var id) || !InFlightByPhysicalConnection.TryGetValue(id, out var inFlight)) return;
      inFlight.TryRemove(operationId, out _);
   }

   static void CloseBefore(Guid? physicalConnection, SqlConnection? connection)
   {
      if(physicalConnection is not {} id || id == Guid.Empty) return;
      if(!InFlightByPhysicalConnection.TryGetValue(id, out var inFlight)) return;
      var closing = new Operation("Close", connection, null);
      foreach(var other in inFlight.Values) ProbeLog.Write(ReportText($"closing physical connection {id} while an operation on it is in progress", closing, other));
   }

   static string HookStatusOf(Operation other) =>
      HookCallsByConnectionObject.TryGetValue(other.ConnectionObject, out var hook)
         ? $"{hook.Calls} call(s) for its SqlConnection, {hook.Removed} removal(s), the last {Stopwatch.GetElapsedTime(hook.Last).TotalMilliseconds:F1} ms ago, {(hook.Last > other.Started ? "after" : "before")} the other began"
         : "never called for its SqlConnection";

   static string ReportText(string what, Operation @this, Operation other) => $"""
                                                                              PROBE SqlClient: {what}
                                                                              ==== This one:
                                                                              {@this}
                                                                              ==== The other, in progress:
                                                                              {other}
                                                                              ==== Hook: {HookStatusOf(other)}
                                                                              """;

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
         if(Interlocked.Exchange(ref _armed, 1) == 0) WriteArmed($"PROBE SqlClient armed in pid {Environment.ProcessId}: the first event is {value.Key}, {value.Value?.GetType().Name}");
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

      static void WriteArmed(string message)
      {
         Console.Error.WriteLine(message);
         var directory = Environment.GetEnvironmentVariable("COMPZE_TEST_DIAGNOSTICS_DIRECTORY");
         if(string.IsNullOrEmpty(directory)) return;
         Directory.CreateDirectory(directory);
         File.AppendAllText(Path.Combine(directory, "probe-armed.log"), message + Environment.NewLine);
      }
   }
}
