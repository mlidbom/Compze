using System.Data.Common;

namespace Compze.Sql.Common;

public static class DbDataReaderCE
{
   public static Guid GetGuidFromString(this DbDataReader @this, int index) => Guid.Parse(@this.GetString(index));
   public static void ForEachSuccessfulRead<TReader>(this TReader @this, Action<TReader> forEach) where TReader : DbDataReader
   {
      while(@this.Read()) forEach(@this);
   }
}