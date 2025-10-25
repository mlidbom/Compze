using System;

namespace Compze.Core.Wiring.Testing.Internal;

/// <summary>
/// Extension methods for SqlLayer to provide convenient access to layer-specific values in tests.
/// </summary>
public static class SqlLayerExtensions
{
   public static TValue ValueFor<TValue>(
      this SqlLayer sqlLayer,
      TValue msSql,
      TValue mySql,
      TValue pgSql,
      TValue sqlite,
      TValue sqliteMemory) where TValue : notnull
   {
      return sqlLayer switch
      {
         SqlLayer.MicrosoftSqlServer => msSql,
         SqlLayer.MySql              => mySql,
         SqlLayer.PostgreSql         => pgSql,
         SqlLayer.Sqlite             => sqlite,
         SqlLayer.SqliteMemory       => sqliteMemory,
         _                           => throw new ArgumentOutOfRangeException(nameof(sqlLayer), sqlLayer, $"Unsupported sql layer: {sqlLayer}")
      };
   }
}
