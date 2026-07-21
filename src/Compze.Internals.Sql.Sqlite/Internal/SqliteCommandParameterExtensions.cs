using System.Data;
using Compze.Internals.SystemCE;
using Microsoft.Data.Sqlite;

namespace Compze.Internals.Sql.Sqlite.Internal;

static class SqliteCommandParameterExtensions
{
   public static SqliteCommand AddParameter(this SqliteCommand @this, string name, int value) => AddParameter(@this, name, SqliteType.Integer, value);
   public static SqliteCommand AddParameter(this SqliteCommand @this, string name, long value) => AddParameter(@this, name, SqliteType.Integer, value);

   // Store DateTime as INTEGER (Ticks) for full precision and efficient storage/comparisons
   public static SqliteCommand AddDateTime2Parameter(this SqliteCommand @this, string name, DateTime value)
      => AddParameter(@this, name, SqliteType.Integer, value.ToUniversalTimeSafely().Ticks);

   public static SqliteCommand AddMediumTextParameter(this SqliteCommand @this, string name, string value) => AddParameter(@this, name, SqliteType.Text, value);

   static SqliteCommand AddParameter(this SqliteCommand @this, SqliteParameter parameter)
   {
      @this.Parameters.Add(parameter);
      return @this;
   }

   static SqliteCommand AddParameter(SqliteCommand @this, string name, SqliteType type, object value) => @this.AddParameter(new SqliteParameter(name, type) {Value = value});

   public static SqliteCommand AddNullableParameter(this SqliteCommand @this, string name, SqliteType type, object? value) => @this.AddParameter(Nullable(new SqliteParameter(name, type) {Value = value}));

   static SqliteParameter Nullable(SqliteParameter @this)
   {
      @this.IsNullable = true;
      @this.Direction = ParameterDirection.Input;
      @this.Value ??= DBNull.Value;
      return @this;
   }
}
