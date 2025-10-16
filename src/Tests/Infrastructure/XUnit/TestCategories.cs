using System;
using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Compze.Tests.Infrastructure.XUnit;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
[TraitDiscoverer("Compze.Tests.Infrastructure.XUnit.LongRunningDiscoverer", "Compze.Tests.Infrastructure.XUnit")]
public sealed class LongRunningAttribute : Attribute, ITraitAttribute
{
}

#pragma warning disable CA1812 // Avoid uninstantiated internal classes : This class is instantiated by xUnit via reflection.
class LongRunningDiscoverer : ITraitDiscoverer
#pragma warning restore CA1812
{
   public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
   {
      yield return new KeyValuePair<string, string>("Category", "LongRunning");
   }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
[TraitDiscoverer("Compze.Tests.Infrastructure.XUnit.PerformanceDiscoverer", "Compze.Tests.Infrastructure.XUnit")]
public sealed class PerformanceAttribute : Attribute, ITraitAttribute
{
}

#pragma warning disable CA1812 // Avoid uninstantiated internal classes : This class is instantiated by xUnit via reflection.
class PerformanceDiscoverer : ITraitDiscoverer
#pragma warning restore CA1812
{
   public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
   {
      yield return new KeyValuePair<string, string>("Category", "Performance");
   }
}
