using System.Runtime.CompilerServices;
using Xunit;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.v3.ComponentPermutations;
#pragma warning disable CA1813 //avoid unsealed attributes

// ReSharper disable GrammarMistakeInComment
/// <summary>
/// Pluggable Components Theory Attribute
/// Use this attribute instead of [XFact] for tests that should run with all pluggable component combinations.
/// Automatically discovers combinations and injects a PluggableComponentTestContext into TestEnv.
/// Use TestEnv to access the component and the information.
/// </summary>
[XunitTestCaseDiscoverer(typeof(PluggableComponentsTheoryDiscoverer))]
public class PluggableComponentsTheoryAttribute(
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1) :
      FactAttribute(sourceFilePath, sourceLineNumber)
{
   static PluggableComponentsTheoryAttribute()
   {
      var expectedTypeName = typeof(PluggableComponentsTheoryDiscoverer).FullName;
      var expectedAssembly = typeof(PluggableComponentsTheoryDiscoverer).Assembly.GetName().Name;
      
      if(expectedTypeName != "Compze.Utilities.Testing.XUnit.v3.ComponentPermutations.PluggableComponentsTheoryDiscoverer")
         throw new Exception($"""
                              PluggableComponentsTheoryDiscoverer type name validation failed
                              Expected: Compze.Utilities.Testing.XUnit.v3.ComponentPermutations.PluggableComponentsTheoryDiscoverer
                              Was: {expectedTypeName}
                              """);
      if(expectedAssembly != "Compze.Utilities.Testing.XUnit.v3")
         throw new Exception($"""
                              PluggableComponentsTheoryDiscoverer assembly name validation failed
                              Expected: Compze.Utilities.Testing.XUnit.v3
                              Was: {expectedAssembly}
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
public sealed class PCTAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1) 
   : PluggableComponentsTheoryAttribute(sourceFilePath, sourceLineNumber) {}
#pragma warning restore CA1813 //avoid unsealed attributes
