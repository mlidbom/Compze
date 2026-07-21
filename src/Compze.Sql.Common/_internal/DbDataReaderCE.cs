namespace Compze.Sql.Common._internal;

static class DbDataReaderCE
{
   public static Guid GetGuidFromString(this DbDataReader @this, int index) => Guid.Parse(@this.GetString(index));

   internal static void ForEachSuccessfulRead<TReader>(this TReader @this, Action<TReader> forEach) where TReader : DbDataReader
   {
      while(@this.Read()) forEach(@this);
   }
}