using System;
using Xunit.Sdk;

namespace Compze.Tests.Infrastructure.XUnit;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
[TraitDiscoverer("Compze.Tests.Infrastructure.XUnit.LongRunningDiscoverer", "Compze.Tests.Infrastructure.XUnit")]
public sealed class LongRunningAttribute : Attribute, ITraitAttribute
{
}
