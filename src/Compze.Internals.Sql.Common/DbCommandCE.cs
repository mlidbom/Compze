using Compze.Contracts;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Internals.Logging;

namespace Compze.Internals.Sql.Common;

public static class DbCommandCE
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

   public static TCommand PrepareStatement<TCommand>(this TCommand @this) where TCommand : DbCommand
   {
      State.Assert(@this.CommandText.Length > 0, () => "Cannot prepare statement with empty CommandText");
      return @this._mutate(me => me.Prepare());
   }

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
