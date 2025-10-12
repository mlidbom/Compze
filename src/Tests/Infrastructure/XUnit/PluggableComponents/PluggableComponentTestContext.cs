using System;
using Compze.Tessaging.Hosting.Testing;
using Compze.Utilities.Logging;
using Xunit.Sdk;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

/// <summary>Here because XUnit requires it. Access context through <see cref="TestEnv"/> for compatibility between NUnit and XUnit</summary>
public class PluggableComponentTestContext : IXunitSerializable
{
   static ILogger Log => CompzeLogger.For<PluggableComponentTestContext>();

   /// <summary>Parameterless constructor required for XUnit serialization.</summary>
   [Obsolete("For XUnit deserialization only")]
   public PluggableComponentTestContext() {}

   internal PluggableComponentTestContext(Infrastructure.PluggableComponents pluggableComponentsCombination)
   {
      Log.NCrunch("PluggableComponentTestContext");
      TestEnv.SetXunitTestContext(pluggableComponentsCombination);
   }

   /// <summary>Serializes this object for XUnit test execution.</summary>
   public void Serialize(IXunitSerializationInfo info) {}

   /// <summary>Deserializes this object from XUnit test execution.</summary>
   public void Deserialize(IXunitSerializationInfo serializerData) =>
      Log.Warning($"NCR:DESERIALIZING {typeof(PluggableComponentTestContext)}");
}
