using System.Data.Common;
using Compze.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Sql.Common.Abstractions;

public interface ICompzeDbConnection
{
   DbCommand CreateCommand();
}

public interface ICompzeDbConnection<out TCommand> : ICompzeDbConnection
   where TCommand : DbCommand
{
   new TCommand CreateCommand();

   void UseCommand(Action<TCommand> action)
   {
      using var command = CreateCommand();
      action(command);
   }

   TResult UseCommand<TResult>(Func<TCommand, TResult> action)
   {
      using var command = CreateCommand();
      return action(command);
   }

   async Task<TResult> UseCommandAsync<TResult>(Func<TCommand, Task<TResult>> action)
   {
      var command = CreateCommand();
      //makes sure DisposeAsync is called without capturing sync context. We can't do it inline because then command would be ConfiguredAsyncDisposable, not TCommand
      await using var _ = command.caf();
      return await action(command).caf();
   }

   int ExecuteNonQuery(string commandText) => UseCommand(command => command.ExecuteNonQuery(commandText));

   async Task<int> ExecuteNonQueryAsync(string commandText) => await UseCommandAsync(async command => await command.ExecuteNonQueryAsync(commandText).caf()).caf();

   object? ExecuteScalar(string commandText) => UseCommand(command => command.ExecuteScalar(commandText));

}