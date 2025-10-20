using Compze.Utilities.SystemCE.ReflectionCE;
using Compze.Wiring.Testing.Sql;
using Xunit;
using Xunit.Sdk;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;
#pragma warning disable CA1813 //avoid unsealed attributes

// ReSharper disable GrammarMistakeInComment
/// <summary>
/// Pluggable Components Theory Attribute
/// Use this attribute instead of [XFact] for tests that should run with all pluggable component combinations.
/// Automatically discovers combinations and injects a PluggableComponentTestContext into TestEnv.
/// Use TestEnv to access the component and the information.
/// </summary>
[XunitTestCaseDiscoverer(PluggableComponentsTheoryAttributeFullTypeName, PluggableComponentsDiscovererAssembly)]
public class PluggableComponentsTheoryAttribute : FactAttribute
{
   const string PluggableComponentsTheoryAttributeFullTypeName = "Compze.Tests.Infrastructure.XUnit.PluggableComponents.PluggableComponentsTheoryDiscoverer";
   const string PluggableComponentsDiscovererAssembly = "Compze.Tests.Infrastructure";

   static PluggableComponentsTheoryAttribute()
   {
      Invariant.Is(PluggableComponentsTheoryAttributeFullTypeName == typeof(PluggableComponentsTheoryDiscoverer).GetFullNameCompilable(),
                   () =>
                      $"""
                       Expected: {typeof(PluggableComponentsTheoryDiscoverer).GetFullNameCompilable()}
                       Found   : {PluggableComponentsTheoryAttributeFullTypeName}
                       """);
      Invariant.Is(PluggableComponentsDiscovererAssembly == typeof(PCTAttribute).Assembly.GetName().Name,
                   () =>
                      $"""
                       Expected: {typeof(PCTAttribute).Assembly.GetName().Name}
                       Found   : {PluggableComponentsDiscovererAssembly}
                       """);
   }

   public SqlLayer[] ExcludeSqlLayers { get; init; } = [];
}

/// <summary>
/// Alias for PluggableComponentsTheoryAttribute
/// Pluggable Components Theory Attribute
/// Use this attribute instead of [XFact] for tests that should run with all pluggable component combinations.
/// Automatically discovers combinations and injects a PluggableComponentTestContext into TestEnv.
/// Use TestEnv to access the component and the information.
/// </summary>
public sealed class PCTAttribute : PluggableComponentsTheoryAttribute {}
#pragma warning restore CA1813 //avoid unsealed attributes
