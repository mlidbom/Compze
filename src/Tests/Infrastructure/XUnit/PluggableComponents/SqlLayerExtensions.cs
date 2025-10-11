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
   /// Only provide values for the sql layers you support.
   /// This is aliased as <see cref="PluggableComponentTestContext.ValueForDb{TValue}"/> for convenience.
   /// </summary>
   public static TValue ValueFor<TValue>(
      this SqlLayer sqlLayer,
      TValue? db2 = default,
      TValue? memory = default,
      TValue? msSql = default,
      TValue? mySql = default,
      TValue? orcl = default,
      TValue? pgSql = default) where TValue : notnull
   {
      return sqlLayer switch
      {
         SqlLayer.MicrosoftSqlServer => SelectValue(msSql, nameof(msSql)),
         SqlLayer.MySql => SelectValue(mySql, nameof(mySql)),
         SqlLayer.PostgreSql => SelectValue(pgSql, nameof(pgSql)),
         _ => throw new ArgumentOutOfRangeException(nameof(sqlLayer), sqlLayer, $"Unsupported sql layer: {sqlLayer}")
      };
   }

   static TValue SelectValue<TValue>(TValue? value, string providerName) where TValue : notnull
   {
      if(!Equals(value, default(TValue))) return Result.ReturnNotNull(value);

      throw new Exception($"Value missing for {providerName}");
   }
}
