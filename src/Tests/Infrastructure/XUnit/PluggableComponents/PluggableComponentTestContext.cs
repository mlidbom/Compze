using System;
using Compze.Utilities.Contracts;
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
   string _combination;
   SqlLayer _sqlLayer;
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
   /// This is an alias for <see cref="SqlLayerExtensions.ValueFor{TValue}"/>.
   /// Prefer using <c>context.SqlLayer.ValueFor(...)</c> for better clarity.
   /// </summary>
   public TValue ValueForDb<TValue>(
      TValue msSql,
      TValue mySql,
      TValue pgSql,
      TValue sqlite) where TValue : notnull =>
      _sqlLayer.ValueFor(msSql: msSql, mySql: mySql, pgSql: pgSql, sqlite: sqlite);

   /// <summary>Serializes this object for XUnit test execution.</summary>
   public void Serialize(IXunitSerializationInfo info) => info.AddValue(nameof(_combination), _combination);

   /// <summary>Deserializes this object from XUnit test execution.</summary>
   public void Deserialize(IXunitSerializationInfo info)
   {
      _combination = info.GetValue<string>(nameof(_combination)).NotNull();

      // Parse and set the enum values
      var parts = _combination.Split(':');

      Assert.Argument.Is(parts.Length == 2, () => $"PluggableComponentParts has an invalid format: {_combination}");

      _sqlLayer = (SqlLayer)Enum.Parse(typeof(SqlLayer), parts[0]);
      _sqlLayer = (SqlLayer)Enum.Parse(typeof(DIContainer), parts[2]);
   }

   public override string ToString() => _combination;
}
