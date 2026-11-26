using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Sql.Common._internal.Abstractions;

interface ICompzeDbConnection<TCommand>
   where TCommand : DbCommand
{
   TCommand CreateCommand();

   ///<summary>As <see cref="CreateCommand"/>, for asynchronous code: a connection whose command creation may wait, as SQLite's does for<br/>
   /// its database's write gate as it takes part in a transaction, waits holding no thread.</summary>
   Task<TCommand> CreateCommandAsync() => Task.FromResult(CreateCommand());

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
      var command = await CreateCommandAsync().caf();
      //makes sure DisposeAsync is called without capturing sync context. We can't do it inline because then command would be ConfiguredAsyncDisposable, not TCommand
      await using var _ = command.caf();
      return await action(command).caf();
   }

   int ExecuteNonQuery(string commandText) => UseCommand(command => command.ExecuteNonQuery(commandText));

   async Task<int> ExecuteNonQueryAsync(string commandText) => await UseCommandAsync(async command => await command.ExecuteNonQueryAsync(commandText).caf()).caf();

   object? ExecuteScalar(string commandText) => UseCommand(command => command.ExecuteScalar(commandText));

   async Task<object?> ExecuteScalarAsync(string commandText) => await UseCommandAsync(async command => await command.SetCommandText(commandText).ExecuteScalarAsync().caf()).caf();
}