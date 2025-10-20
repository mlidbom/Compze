using Xunit;
using Xunit.Sdk;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;
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
   const string PluggableComponentsTheoryAttributeFullTypeName = $"Compze.Utilities.Testing.XUnit.ComponentPermutations.{nameof(PluggableComponentsTheoryDiscoverer)}";
   const string PluggableComponentsDiscovererAssembly = "Compze.Utilities.Testing.XUnit";

   static PluggableComponentsTheoryAttribute()
   {
      if(PluggableComponentsTheoryAttributeFullTypeName != typeof(PluggableComponentsTheoryDiscoverer).FullName)
         throw new Exception($"""
                              {PluggableComponentsTheoryAttributeFullTypeName} is not the actual type name.
                              Should be: {typeof(PluggableComponentsTheoryDiscoverer).FullName}
                              Was   : {PluggableComponentsTheoryAttributeFullTypeName}
                              """);
      if(PluggableComponentsDiscovererAssembly != typeof(PCTAttribute).Assembly.GetName().Name)
         throw new Exception($"""
                              {PluggableComponentsDiscovererAssembly} is not the actual assembly name.
                              Should be: {typeof(PCTAttribute).Assembly.GetName().Name}
                              Was   : {PluggableComponentsDiscovererAssembly}
                              """);
   }

   public string[] Exclude { get; init; } = [];
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
