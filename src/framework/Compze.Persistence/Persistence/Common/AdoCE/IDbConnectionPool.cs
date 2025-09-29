using System;
using System.Data.Common;
using System.Threading.Tasks;
using Compze.SystemCE;
using Compze.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Persistence.Common.AdoCE;

interface IDbConnectionPool<out TConnection, out TCommand>
   where TConnection : IPoolableConnection, ICompzeDbConnection<TCommand>
   where TCommand : DbCommand
{
   TResult UseConnection<TResult>(Func<TConnection, TResult> func);

   void UseConnection(Action<TConnection> action) => UseConnection(action.AsUnitFunc());

   async Task UseConnectionAsync(Func<TConnection, Task> func) => await UseConnectionAsync(func.AsUnitFunc()).CaF();

   Task<TResult> UseConnectionAsync<TResult>(Func<TConnection, Task<TResult>> func);

   int ExecuteNonQuery(string commandText) =>
      UseConnection(connection => connection.ExecuteNonQuery(commandText));

   object? ExecuteScalar(string commandText) =>
      UseConnection(connection => connection.ExecuteScalar(commandText));

   async Task<int> ExecuteNonQueryAsync(string commandText) =>
      await UseConnectionAsync(async connection => await connection.ExecuteNonQueryAsync(commandText).CaF()).CaF();

   int PrepareAndExecuteNonQuery(string commandText) =>
      UseConnection(connection => connection.PrepareAndExecuteNonQuery(commandText));

   async Task<int> PrepareAndExecuteNonQueryAsync(string commandText) =>
      await UseConnectionAsync(async connection => await connection.PrepareAndExecuteNonQueryAsync(commandText).CaF()).CaF();

   void UseCommand(Action<TCommand> action) =>
      UseConnection(connection => connection.UseCommand(action));

   TResult UseCommand<TResult>(Func<TCommand, TResult> action) =>
      UseConnection(connection => connection.UseCommand(action));
}
