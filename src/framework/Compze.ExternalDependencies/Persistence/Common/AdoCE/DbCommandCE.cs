using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Compze.Contracts;
using Compze.Functional;
using Compze.Logging;
using Compze.SystemCE;
using Compze.SystemCE.LinqCE;
using Compze.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Persistence.Common.AdoCE;

static class DbCommandCE
{
   public static object? ExecuteScalar(this DbCommand @this, string commandText) =>
      @this.SetCommandText(commandText).ExecuteScalar();

   public static async Task<object?> ExecuteScalarAsync(this DbCommand @this, string commandText) =>
      await @this.SetCommandText(commandText).ExecuteScalarAsync().CaF();

   public static int ExecuteNonQuery(this DbCommand @this, string commandText) =>
      @this.SetCommandText(commandText).ExecuteNonQuery();

   public static async Task<int> ExecuteNonQueryAsync(this DbCommand @this, string commandText) =>
      await @this.SetCommandText(commandText).ExecuteNonQueryAsync().CaF();


   public static object? PrepareAndExecuteScalar(this DbCommand @this, string commandText) =>
      @this.SetCommandText(commandText).PrepareStatement().ExecuteScalar();

   public static int PrepareAndExecuteNonQuery(this DbCommand @this, string commandText) =>
      @this.SetCommandText(commandText).PrepareStatement().ExecuteNonQuery();

   public static async Task<int> PrepareAndExecuteNonQueryAsync(this DbCommand @this, string commandText) =>
      await @this.SetCommandText(commandText).PrepareStatement().ExecuteNonQueryAsync().CaF();

   public static TCommand AppendCommandText<TCommand>(this TCommand @this, string append) where TCommand : DbCommand =>
      @this.mutate(me => me.CommandText += append);

   public static TCommand SetCommandText<TCommand>(this TCommand @this, string commandText) where TCommand : DbCommand =>
      @this.mutate(me => me.CommandText = commandText);

   public static TCommand PrepareStatement<TCommand>(this TCommand @this) where TCommand : DbCommand
   {
      Assert.State.Is(@this.CommandText.Length > 0, () => "Cannot prepare statement with empty CommandText");
      return @this.mutate(me => me.Prepare());
   }

   public static async Task<TCommand> PrepareStatementAsync<TCommand>(this TCommand @this) where TCommand : DbCommand
   {
      Assert.State.Is(@this.CommandText.Length > 0, () => "Cannot prepare statement with empty CommandText");
      return await @this.mutateAsync(async me => await me.PrepareAsync().CaF()).CaF();
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
      ConsoleCE.WriteLine("####################################### Logging command###############################################");
      ConsoleCE.WriteLine($"""
                           {nameof(@this.CommandText)}:
                           {@this.CommandText}
                           """);

      var parameters = @this.Parameters.Cast<DbParameter>().ToList();

      if(parameters.Any())
      {
         parameters.ForEach(
            parameter => Console.WriteLine($"""
                                            
                                                {nameof(parameter.ParameterName)}: {parameter.ParameterName}, 
                                            {nameof(parameter.DbType)}: {parameter.DbType},
                                            {nameof(parameter.Value)}: {parameter.Value},
                                            {nameof(parameter.Size)}: {parameter.Size},
                                            {nameof(parameter.Precision)}: {parameter.Precision},
                                            {nameof(parameter.Direction)}: {parameter.Direction},
                                            {nameof(parameter.IsNullable)}: {parameter.IsNullable}
                                            """.ReplaceInvariant(Environment.NewLine, "")));

         ConsoleCE.WriteLine("####################################### Hacking values into parameter positions #######################################");
         var commandTextWithParameterValues = @this.CommandText;
         parameters.ForEach(
            parameter => ParameterPrefixes.ForEach(
               prefix => commandTextWithParameterValues = commandTextWithParameterValues.ReplaceInvariant($"{prefix}{parameter.ParameterName}", parameter.Value == DBNull.Value ? "NULL" : parameter.Value?.ToString() ?? "NULL")));
         Console.WriteLine(commandTextWithParameterValues);
         ConsoleCE.WriteLine("######################################################################################################");
      }

      return @this;
   }
}