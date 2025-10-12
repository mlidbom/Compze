using System;
using Compze.Tessaging.Hosting.Testing;
using Compze.Utilities.SystemCE;
using Xunit.Sdk;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

/// <summary>Really nothing to see here. You have to take it as a parameter but You won't use it for anything. <see cref="TestEnv"/> is where you find everything</summary>
public class PluggableComponentTestContext : IXunitSerializable
{
   Infrastructure.PluggableComponents? _combination;

   [Obsolete("For XUnit deserialization only")]
   public PluggableComponentTestContext() => _combination = null;

   internal PluggableComponentTestContext(Infrastructure.PluggableComponents pluggableComponentsCombination) =>
      _combination = pluggableComponentsCombination;

   public void Serialize(IXunitSerializationInfo info) =>
      info.AddValue(nameof(_combination), _combination.ToString());

   public void Deserialize(IXunitSerializationInfo serializerData) =>
      _combination = Infrastructure.PluggableComponents.FromString(serializerData.GetValue<string>(nameof(_combination)).NotNull());

   public override string ToString() => _combination!.Value.ToString();
}
