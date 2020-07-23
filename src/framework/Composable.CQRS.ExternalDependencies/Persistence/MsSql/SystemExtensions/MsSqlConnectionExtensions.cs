using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Persistence.MsSql.SystemExtensions
{
    static class MsSqlConnectionExtensions
    {
        public static void UseCommand(this SqlConnection @this, Action<SqlCommand> action)
        {
            using var command = @this.CreateCommand();
            action(command);
        }

        public static TResult UseCommand<TResult>(this SqlConnection @this, Func<SqlCommand, TResult> action)
        {
            using var command = @this.CreateCommand();
            return action(command);
        }

        public static void ExecuteNonQuery(this SqlConnection @this, string commandText) => @this.UseCommand(command => command.ExecuteNonQuery(commandText));
        public static async Task<int> ExecuteNonQueryAsync(this SqlConnection @this, string commandText) => await @this.UseCommand(command => command.ExecuteNonQueryAsync(commandText)).NoMarshalling();
        public static object ExecuteScalar(this SqlConnection @this, string commandText) => @this.UseCommand(command => command.ExecuteScalar(commandText));
        public static Task<object> ExecuteScalarAsync(this SqlConnection @this, string commandText) => @this.UseCommand(command => command.ExecuteScalarAsync(commandText));
        public static void ExecuteReader(this SqlConnection @this, string commandText, Action<SqlDataReader> forEach) => @this.UseCommand(command => command.ExecuteReader(commandText, forEach));
    }
}
