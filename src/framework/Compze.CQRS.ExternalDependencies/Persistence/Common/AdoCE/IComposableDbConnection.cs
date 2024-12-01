using System;
using System.Data.Common;
using System.Threading.Tasks;
using Compze.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Persistence.Common.AdoCE;

interface ICompzDbConnection
{
   DbCommand CreateCommand();
}

interface ICompzDbConnection<out TCommand> : ICompzDbConnection
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

   async Task UseCommandAsync(Func<TCommand, Task> action)
   {
      var command = CreateCommand();
      await using var command1 = command.CaF();
      await action(command).CaF();
   }

   int ExecuteNonQuery(string commandText) => UseCommand(command => command.ExecuteNonQuery(commandText));

   async Task<int> ExecuteNonQueryAsync(string commandText) => await UseCommand(command => command.ExecuteNonQueryAsync(commandText)).CaF();

   object? ExecuteScalar(string commandText) => UseCommand(command => command.ExecuteScalar(commandText));

   Task<object?> ExecuteScalarAsync(string commandText) => UseCommand(command => command.ExecuteScalarAsync(commandText));

   int PrepareAndExecuteNonQuery(string commandText) => UseCommand(command => command.PrepareAndExecuteNonQuery(commandText));

   async Task<int> PrepareAndExecuteNonQueryAsync(string commandText) =>
      await UseCommand(command => command.PrepareAndExecuteNonQueryAsync(commandText)).CaF();

   object? PrepareAndExecuteScalar(string commandText) => UseCommand(command => command.PrepareAndExecuteScalar(commandText));
}