using System;
using Compze.Wiring;
using Xunit.Sdk;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

/// <summary>
/// Instance-based test context that provides access to the current pluggable component combination.
/// This is an alternative to the static TestEnv class, designed for dependency injection into test methods.
/// </summary>
public class PluggableComponentTestContext : IXunitSerializable
{
   readonly string _combination;
   readonly SqlLayer _sqlLayer;
   readonly DIContainer _diContainer;

   /// <summary>
   /// Parameterless constructor required for XUnit serialization.
   /// </summary>
   [Obsolete("For XUnit deserialization only")]
   public PluggableComponentTestContext()
   {
      _combination = string.Empty;
      _sqlLayer = default;
      _diContainer = default;
   }

   public PluggableComponentTestContext(string pluggableComponentsCombination)
   {
      _combination = pluggableComponentsCombination;

      var parts = pluggableComponentsCombination.Split(':');
      if(parts.Length != 2)
         throw new ArgumentException($"Invalid combination format: {pluggableComponentsCombination}. Expected format: 'SqlLayer:DIContainer'");

      if(!Enum.TryParse(parts[0], out _sqlLayer))
         throw new ArgumentException($"Invalid sql layer: {parts[0]}");

      if(!Enum.TryParse(parts[1], out _diContainer))
         throw new ArgumentException($"Invalid DI container: {parts[1]}");
   }

   /// <summary>Gets the full combination string (e.g., "MicrosoftSqlServer:Microsoft")</summary>
   public string Combination => _combination;

   /// <summary>Gets the current sql layer for this test</summary>
   public SqlLayer SqlLayer => _sqlLayer;

   /// <summary>Gets the current DI container for this test</summary>
   public DIContainer DIContainer => _diContainer;

   /// <summary>
   /// Returns a sql-layer-specific value.
   /// Only provide values for the sql layers you support.
   /// This is an alias for <see cref="SqlLayerExtensions.ValueFor{TValue}"/>.
   /// Prefer using <c>context.SqlLayer.ValueFor(...)</c> for better clarity.
   /// </summary>
   public TValue ValueForDb<TValue>(
      TValue? db2 = default,
      TValue? memory = default,
      TValue? msSql = default,
      TValue? mySql = default,
      TValue? orcl = default,
      TValue? pgSql = default) where TValue : notnull
   {
      return _sqlLayer.ValueFor(db2: db2, memory: memory, msSql: msSql, mySql: mySql, orcl: orcl, pgSql: pgSql);
   }

   /// <summary>Serializes this object for XUnit test execution.</summary>
   public void Serialize(IXunitSerializationInfo info)
   {
      info.AddValue(nameof(_combination), _combination);
   }

   /// <summary>Deserializes this object from XUnit test execution.</summary>
   public void Deserialize(IXunitSerializationInfo info)
   {
      var combination = info.GetValue<string>(nameof(_combination))
                     ?? throw new InvalidOperationException("Combination string was null during deserialization");

      // Use reflection to set the readonly fields since we're deserializing
      var combinationField = typeof(PluggableComponentTestContext).GetField(nameof(_combination),
                                                                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
      combinationField.SetValue(this, combination);

      // Parse and set the enum values
      var parts = combination.Split(':');
      if(parts.Length == 2)
      {
         if(Enum.TryParse(parts[0], out SqlLayer sqlLayer))
         {
            var sqlLayerField = typeof(PluggableComponentTestContext).GetField(nameof(_sqlLayer),
                                                                                       System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            sqlLayerField.SetValue(this, sqlLayer);
         }

         if(Enum.TryParse(parts[1], out DIContainer diContainer))
         {
            var diContainerField = typeof(PluggableComponentTestContext).GetField(nameof(_diContainer),
                                                                                  System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            diContainerField.SetValue(this, diContainer);
         }
      }
   }

   public override string ToString() => _combination;
}
