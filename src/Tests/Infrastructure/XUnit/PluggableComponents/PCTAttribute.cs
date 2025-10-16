using Compze.Utilities.SystemCE.ReflectionCE;
using Xunit;
using Xunit.Sdk;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

//XUnit.v3 version ready to go once v3 is stable in NCrunch is at git commit: deb6be8d66ec03db2a55f84ff28feab220ae50b1
/// <summary>
/// Pluggable Components Theory Attribute
/// Use this attribute instead of [XFact] for tests that should run with all pluggable component combinations.
/// Automatically discovers combinations and injects a PluggableComponentTestContext into TestEnv.
/// </summary>
[XunitTestCaseDiscoverer(PluggableComponentsTheoryAttributeFullTypeName, PluggableComponentsDiscovererAssembly)]
public sealed class PCTAttribute : FactAttribute
{
   const string PluggableComponentsTheoryAttributeFullTypeName = "Compze.Tests.Infrastructure.XUnit.PluggableComponents.PluggableComponentsTheoryDiscoverer";
   const string PluggableComponentsDiscovererAssembly = "Compze.Tests.Infrastructure.XUnit";

   static PCTAttribute()
   {
      Invariant.Is(PluggableComponentsTheoryAttributeFullTypeName == typeof(PluggableComponentsTheoryDiscoverer).GetFullNameCompilable());
      Invariant.Is(PluggableComponentsDiscovererAssembly == typeof(PCTAttribute).Assembly.GetName().Name);
   }

   public Wiring.SqlLayer[] ExcludeSqlLayers { get; init; } = [];
}
#pragma warning disable CA1812 // Avoid uninstantiated internal classes : This class is instantiated by xUnit via reflection.
