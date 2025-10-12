using System;
using Compze.Tessaging.Hosting.Testing;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE;
using Compze.Wiring;
using Xunit.Sdk;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

/// <summary>
/// Instance-based test context that provides access to the current pluggable component combination.
/// This is an alternative to the static TestEnv class, designed for dependency injection into test methods.
/// </summary>
public class PluggableComponentTestContext : IXunitSerializable
{
   static ILogger Log => CompzeLogger.For<PluggableComponentTestContext>();
   Infrastructure.PluggableComponents? _combination;

   /// <summary>Parameterless constructor required for XUnit serialization.</summary>
   [Obsolete("For XUnit deserialization only")]
   public PluggableComponentTestContext() => _combination = null;

   internal PluggableComponentTestContext(Infrastructure.PluggableComponents pluggableComponentsCombination)
   {
      Log.NCrunch("PluggableComponentTestContext");
      TestEnv.SetXunitTestContext(pluggableComponentsCombination);
      _combination = pluggableComponentsCombination;
   }

   public Infrastructure.PluggableComponents Combination => _combination!.Value;

   /// <summary>Gets the current sql layer for this test</summary>
   public SqlLayer SqlLayer => Combination.SqlLayer;

   /// <summary>Gets the current DI container for this test</summary>
   public DIContainer DIContainer => Combination.DiContainer;

   /// <summary>
   /// Returns a sql-layer-specific value.
   /// This is an alias for <see cref="SqlLayerExtensions.ValueFor{TValue}"/>.
   /// Prefer using <c>context.SqlLayer.ValueFor(...)</c> for better clarity.
   /// </summary>
   public TValue ValueForDb<TValue>(TValue msSql, TValue mySql, TValue pgSql, TValue sqlite) where TValue : notnull =>
      SqlLayer.ValueFor(msSql: msSql, mySql: mySql, pgSql: pgSql, sqlite: sqlite);

   /// <summary>Serializes this object for XUnit test execution.</summary>
   public void Serialize(IXunitSerializationInfo info)
   {
      Log.Warning($"NCR:SERIALIZING {_combination}");
      info.AddValue(nameof(_combination), _combination.ToString());
   }

   /// <summary>Deserializes this object from XUnit test execution.</summary>
   public void Deserialize(IXunitSerializationInfo serializerData)
   {
      Log.Warning($"NCR:DESERIALIZING {_combination}");
      _combination = Infrastructure.PluggableComponents.FromString(serializerData.GetValue<string>(nameof(_combination)).NotNull());
      TestEnv.SetXunitTestContext(_combination!.Value);
   }

   public override string ToString() => _combination!.Value.ToString();
}
