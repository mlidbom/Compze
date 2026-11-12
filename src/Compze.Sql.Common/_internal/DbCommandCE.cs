using Compze.Contracts;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Sql.Common._internal;

static class DbCommandCE
{
   static readonly ILogger Log = CompzeLogger.For(typeof(DbCommandCE));

   internal static object? ExecuteScalar(this DbCommand @this, string commandText) =>
      @this.SetCommandText(commandText).ExecuteScalar();

   internal static int ExecuteNonQuery(this DbCommand @this, string commandText) =>
      @this.SetCommandText(commandText).ExecuteNonQuery();

   internal static async Task<int> ExecuteNonQueryAsync(this DbCommand @this, string commandText) =>
      await @this.SetCommandText(commandText).ExecuteNonQueryAsync().caf();

   public static TCommand AppendCommandText<TCommand>(this TCommand @this, string append) where TCommand : DbCommand =>
      @this._mutate(me => me.CommandText += append);

   public static TCommand SetCommandText<TCommand>(this TCommand @this, string commandText) where TCommand : DbCommand =>
      @this._mutate(me => me.CommandText = commandText);

   ///<summary>Prepares the statement on the server, synchronously: for synchronous code only. Async code uses<br/>
   /// <see cref="ExecutePreparedNonQueryAsync{TCommand}"/> and its siblings, which prepare without blocking a thread.</summary>
   public static TCommand PrepareStatement<TCommand>(this TCommand @this) where TCommand : DbCommand
   {
      AssertHasCommandText(@this);
      return @this._mutate(me => me.Prepare());
   }

   ///<summary>Prepares the statement on the server, then executes it and returns the number of rows affected — asynchronously<br/>
   /// throughout. Async code must never call <see cref="PrepareStatement{TCommand}"/>: preparing is a round trip to the server, and<br/>
   /// a synchronous one blocks a thread-pool thread for its whole duration.</summary>
   ///<remarks>Found 2026-11-12: on a 4-processor Linux CI runner whose thread pool was starved, a PostgreSQL peer advertisement<br/>
   /// recording hung for over 10 seconds in a synchronous prepare's socket read, while the server logged no statement slower<br/>
   /// than a second.</remarks>
   public static async Task<int> ExecutePreparedNonQueryAsync<TCommand>(this TCommand @this) where TCommand : DbCommand
   {
      await PrepareStatementAsync(@this).caf();
      return await @this.ExecuteNonQueryAsync().caf();
   }

   ///<summary>Like <see cref="ExecutePreparedNonQueryAsync{TCommand}"/>, returning the first column of the first row.</summary>
   public static async Task<object?> ExecutePreparedScalarAsync<TCommand>(this TCommand @this) where TCommand : DbCommand
   {
      await PrepareStatementAsync(@this).caf();
      return await @this.ExecuteScalarAsync().caf();
   }

   ///<summary>Like <see cref="ExecutePreparedNonQueryAsync{TCommand}"/>, returning a reader over the result rows.</summary>
   public static async Task<DbDataReader> ExecutePreparedReaderAsync<TCommand>(this TCommand @this) where TCommand : DbCommand
   {
      await PrepareStatementAsync(@this).caf();
      return await @this.ExecuteReaderAsync().caf();
   }

   static async Task PrepareStatementAsync(DbCommand command)
   {
      AssertHasCommandText(command);
      await command.PrepareAsync().caf();
   }

   static void AssertHasCommandText(DbCommand command) => State.Assert(command.CommandText.Length > 0, () => "Cannot prepare statement with empty CommandText");

   public static IReadOnlyList<T> ExecuteReaderAndSelect<T, TCommand, TReader>(this TCommand @this, Func<TReader, T> select)
      where TCommand : DbCommand
      where TReader : DbDataReader
   {
      using var reader = (TReader)@this.ExecuteReader();
      var result = new List<T>();
      reader.ForEachSuccessfulRead(row => result.Add(select(row)));
      return result;
   }

   static readonly IReadOnlyList<string> ParameterPrefixes = ["@", ":"];

   // ReSharper disable once UnusedMember.Global : Only used when debugging,but very useful then
   public static TCommand LogCommand<TCommand>(this TCommand @this) where TCommand : DbCommand
   {
      Log.Info("####################################### Logging command###############################################");
      Log.Info($"""
                {nameof(@this.CommandText)}:
                {@this.CommandText}
                """);

      var parameters = @this.Parameters.Cast<DbParameter>().ToList();

      if(parameters.Any())
      {
         parameters.ForEach(parameter => Log.Info($"""

                                                   {nameof(parameter.ParameterName)}: {parameter.ParameterName}, 
                                                   {nameof(parameter.DbType)}: {parameter.DbType},
                                                   {nameof(parameter.Value)}: {parameter.Value},
                                                   {nameof(parameter.Size)}: {parameter.Size},
                                                   {nameof(parameter.Precision)}: {parameter.Precision},
                                                   {nameof(parameter.Direction)}: {parameter.Direction},
                                                   {nameof(parameter.IsNullable)}: {parameter.IsNullable}
                                                   """.ReplaceOrdinal(Environment.NewLine, "")));

         Log.Info("####################################### Hacking values into parameter positions #######################################");
         var commandTextWithParameterValues = @this.CommandText;
         parameters.ForEach(parameter => ParameterPrefixes.ForEach(prefix => commandTextWithParameterValues = commandTextWithParameterValues.ReplaceOrdinal($"{prefix}{parameter.ParameterName}", parameter.Value == DBNull.Value ? "NULL" : parameter.Value?.ToString() ?? "NULL")));
         Log.Info(commandTextWithParameterValues);
         Log.Info("######################################################################################################");
      }

      return @this;
   }
}
