using System.Runtime.CompilerServices;
using Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;
using Compze.Utilities.SystemCE.ReflectionCE;
using Xunit;
using Xunit.Sdk;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

/// <summary>
/// Use this attribute instead of [Fact] for tests that should run with all pluggable component combinations.
/// Automatically discovers combinations and injects a PluggableComponentTestContext into TestEnv.
/// </summary>
[XunitTestCaseDiscoverer(PluggableComponentsTheoryAttributeFullTypeName, PluggableComponentsDiscovererAssembly)]
public sealed class PluggableComponentsTheoryAttribute : FactAttribute
{
   const string PluggableComponentsTheoryAttributeFullTypeName = "Compze.Tests.Infrastructure.XUnit.PluggableComponents.PluggableComponentsTheoryDiscoverer";
   const string PluggableComponentsDiscovererAssembly = "Compze.Tests.Infrastructure.XUnit";

   static PluggableComponentsTheoryAttribute()
   {
      Invariant.Is(PluggableComponentsTheoryAttributeFullTypeName == typeof(PluggableComponentsTheoryDiscoverer).GetFullNameCompilable());
      Invariant.Is(PluggableComponentsDiscovererAssembly == typeof(PluggableComponentsTheoryAttribute).Assembly.GetName().Name);
   }

   public Wiring.SqlLayer[] ExcludeSqlLayers { get; init; } = [];
}
#pragma warning disable CA1812 // Avoid uninstantiated internal classes : This class is instantiated by xUnit via reflection.
