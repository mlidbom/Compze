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
   readonly PersistenceLayer _persistenceLayer;
   readonly DIContainer _diContainer;

   /// <summary>
   /// Parameterless constructor required for XUnit serialization.
   /// </summary>
   [Obsolete("For XUnit deserialization only")]
   public PluggableComponentTestContext()
   {
      _combination = string.Empty;
      _persistenceLayer = default;
      _diContainer = default;
   }

   public PluggableComponentTestContext(string pluggableComponentsCombination)
   {
      _combination = pluggableComponentsCombination;

      var parts = pluggableComponentsCombination.Split(':');
      if(parts.Length != 2)
         throw new ArgumentException($"Invalid combination format: {pluggableComponentsCombination}. Expected format: 'PersistenceLayer:DIContainer'");

      if(!Enum.TryParse(parts[0], out _persistenceLayer))
         throw new ArgumentException($"Invalid persistence layer: {parts[0]}");

      if(!Enum.TryParse(parts[1], out _diContainer))
         throw new ArgumentException($"Invalid DI container: {parts[1]}");
   }

   /// <summary>Gets the full combination string (e.g., "MicrosoftSqlServer:Microsoft")</summary>
   public string Combination => _combination;

   /// <summary>Gets the current persistence layer for this test</summary>
   public PersistenceLayer PersistenceLayer => _persistenceLayer;

   /// <summary>Gets the current DI container for this test</summary>
   public DIContainer DIContainer => _diContainer;

   /// <summary>
   /// Returns a persistence-layer-specific value.
   /// Only provide values for the persistence layers you support.
   /// This is an alias for <see cref="PersistenceLayerExtensions.ValueFor{TValue}"/>.
   /// Prefer using <c>context.PersistenceLayer.ValueFor(...)</c> for better clarity.
   /// </summary>
   public TValue ValueForDb<TValue>(
      TValue? db2 = default,
      TValue? memory = default,
      TValue? msSql = default,
      TValue? mySql = default,
      TValue? orcl = default,
      TValue? pgSql = default) where TValue : notnull
   {
      return _persistenceLayer.ValueFor(db2: db2, memory: memory, msSql: msSql, mySql: mySql, orcl: orcl, pgSql: pgSql);
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
         if(Enum.TryParse(parts[0], out PersistenceLayer persistenceLayer))
         {
            var persistenceLayerField = typeof(PluggableComponentTestContext).GetField(nameof(_persistenceLayer),
                                                                                       System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            persistenceLayerField.SetValue(this, persistenceLayer);
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
