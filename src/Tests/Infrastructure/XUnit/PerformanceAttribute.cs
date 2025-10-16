using System;
using Xunit.Sdk;

namespace Compze.Tests.Infrastructure.XUnit;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
[TraitDiscoverer("Compze.Tests.Infrastructure.XUnit.PerformanceDiscoverer", "Compze.Tests.Infrastructure.XUnit")]
public sealed class PerformanceAttribute : Attribute, ITraitAttribute
{
}
