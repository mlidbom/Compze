using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.SystemCE;

namespace Compze.Internals.Sql.Common.Abstractions;

public interface IDbConnectionPool<out TConnection, out TCommand>
   where TConnection : IPoolableConnection, ICompzeDbConnection<TCommand>
   where TCommand : DbCommand
{
   TResult UseConnection<TResult>(Func<TConnection, TResult> func);

   void UseConnection(Action<TConnection> action) => UseConnection(action.ToFunc());

   Task<TResult> UseConnectionAsync<TResult>(Func<TConnection, Task<TResult>> func);

   int ExecuteNonQuery(string commandText) =>
      UseConnection(connection => connection.ExecuteNonQuery(commandText));

   object? ExecuteScalar(string commandText) =>
      UseConnection(connection => connection.ExecuteScalar(commandText));

   async Task<int> ExecuteNonQueryAsync(string commandText) =>
      await UseConnectionAsync(async connection => await connection.ExecuteNonQueryAsync(commandText).caf()).caf();

   void UseCommand(Action<TCommand> action) =>
      UseConnection(connection => connection.UseCommand(action));

   TResult UseCommand<TResult>(Func<TCommand, TResult> action) =>
      UseConnection(connection => connection.UseCommand(action));

   async Task<TResult> UseCommandAsync<TResult>(Func<TCommand, Task<TResult>> action) =>
      await UseConnectionAsync(async connection => await connection.UseCommandAsync(action).caf()).caf();
}
