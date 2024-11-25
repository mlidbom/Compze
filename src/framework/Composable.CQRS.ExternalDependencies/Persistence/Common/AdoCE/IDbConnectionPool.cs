using System;
using System.Data.Common;
using System.Threading.Tasks;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.Persistence.Common.AdoCE;

interface IDbConnectionPool<out TConnection, out TCommand>
   where TConnection : IPoolableConnection, IComposableDbConnection<TCommand>
   where TCommand : DbCommand
{
   Task<TResult> UseConnectionAsyncFlex<TResult>(SyncOrAsync syncOrAsync, Func<TConnection, Task<TResult>> func);

   TResult UseConnection<TResult>(Func<TConnection, TResult> func) =>
      UseConnectionAsyncFlex(SyncOrAsync.Sync, func.AsAsync()).SyncResult();

   void UseConnection(Action<TConnection> action) =>
      UseConnectionAsyncFlex(SyncOrAsync.Sync, action.AsVoidFunc().AsAsync()).SyncResult();

   async Task UseConnectionAsync(Func<TConnection, Task> action) =>
      await UseConnectionAsyncFlex(SyncOrAsync.Async, action.AsVoidFunc()).CaF();

   async Task<TResult> UseConnectionAsync<TResult>(Func<TConnection, Task<TResult>> func) =>
      await UseConnectionAsyncFlex(SyncOrAsync.Async, func).CaF();

   int ExecuteNonQuery(string commandText) =>
      UseConnection(connection => connection.ExecuteNonQuery(commandText));

   object? ExecuteScalar(string commandText) =>
      UseConnection(connection => connection.ExecuteScalar(commandText));

   async Task<int> ExecuteNonQueryAsync(string commandText) =>
      await UseConnectionAsync(async connection => await connection.ExecuteNonQueryAsync(commandText).CaF()).CaF();

   int PrepareAndExecuteNonQuery(string commandText) =>
      UseConnection(connection => connection.PrepareAndExecuteNonQuery(commandText));

   object? PrepareAndExecuteScalar(string commandText) =>
      UseConnection(connection => connection.PrepareAndExecuteScalar(commandText));

   async Task<int> PrepareAndExecuteNonQueryAsync(string commandText) =>
      await UseConnectionAsync(async connection => await connection.PrepareAndExecuteNonQueryAsync(commandText).CaF()).CaF();

   void UseCommand(Action<TCommand> action) =>
      UseConnection(connection => connection.UseCommand(action));

   Task UseCommandAsync(Func<TCommand, Task> action) =>
      UseConnectionAsync(async connection => await connection.UseCommandAsync(action).CaF());

   TResult UseCommand<TResult>(Func<TCommand, TResult> action) =>
      UseConnection(connection => connection.UseCommand(action));
}