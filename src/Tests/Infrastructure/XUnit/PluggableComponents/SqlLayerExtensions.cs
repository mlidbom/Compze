using System;
using Compze.Wiring;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

/// <summary>
/// Extension methods for SqlLayer to provide convenient access to layer-specific values in tests.
/// </summary>
public static class SqlLayerExtensions
{
   /// <summary>
   /// Returns a sql-layer-specific value.
   /// All SQL layers must be provided - no defaults allowed.
   /// This is aliased as <see cref="PluggableComponentTestContext.ValueForDb{TValue}"/> for convenience.
   /// </summary>
   public static TValue ValueFor<TValue>(
      this SqlLayer sqlLayer,
      TValue msSql,
      TValue mySql,
      TValue pgSql,
      TValue sqlite) where TValue : notnull
   {
      return sqlLayer switch
      {
         SqlLayer.MicrosoftSqlServer => msSql,
         SqlLayer.MySql => mySql,
         SqlLayer.PostgreSql => pgSql,
         SqlLayer.Sqlite => sqlite,
         SqlLayer.SqliteMemory => sqlite,
         _ => throw new ArgumentOutOfRangeException(nameof(sqlLayer), sqlLayer, $"Unsupported sql layer: {sqlLayer}")
      };
   }
}
