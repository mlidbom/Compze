using System;
using System.Diagnostics.CodeAnalysis;
using Compze.Wiring;

namespace Compze.Tests.Infrastructure.XUnit;

/// <summary>
/// Extension methods for PersistenceLayer to provide convenient access to layer-specific values in tests.
/// </summary>
public static class PersistenceLayerExtensions
{
   /// <summary>
   /// Returns a persistence-layer-specific value.
   /// Only provide values for the persistence layers you support.
   /// This is aliased as <see cref="PluggableComponentTestContext.ValueForDb{TValue}"/> for convenience.
   /// </summary>
   public static TValue ValueFor<TValue>(
      this PersistenceLayer persistenceLayer,
      TValue? db2 = default,
      TValue? memory = default,
      TValue? msSql = default,
      TValue? mySql = default,
      TValue? orcl = default,
      TValue? pgSql = default) where TValue : notnull
   {
      return persistenceLayer switch
      {
         PersistenceLayer.MicrosoftSqlServer => SelectValue(msSql, nameof(msSql)),
         PersistenceLayer.Memory => SelectValue(memory, nameof(memory)),
         PersistenceLayer.MySql => SelectValue(mySql, nameof(mySql)),
         PersistenceLayer.PostgreSql => SelectValue(pgSql, nameof(pgSql)),
         _ => throw new ArgumentOutOfRangeException(nameof(persistenceLayer), persistenceLayer, $"Unsupported persistence layer: {persistenceLayer}")
      };
   }

   [return: NotNull]
   static TValue SelectValue<TValue>(TValue? value, string providerName) where TValue : notnull
   {
      if(value != null && !Equals(value, default(TValue)))
         return value;

      throw new InvalidOperationException($"No value provided for {providerName}");
   }
}
