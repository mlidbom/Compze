using System;
using Compze.Utilities.Functional;
using Compze.Utilities.Testing.XUnit.ComponentCombinations;

namespace Compze.Tests.Infrastructure.XUnit;

static class TestCasePluggableComponentsExtractor
{
   public static Tessaging.Hosting.Testing.PluggableComponents ToPluggableComponents(this ComponentCombination? combination) =>
      combination.TryExtractPluggableComponents(throwOnFailure: true)!.Value;

   public static Tessaging.Hosting.Testing.PluggableComponents? TryExtractPluggableComponents(this ComponentCombination? combination,
                                                                                              bool throwOnFailure = false)
   {
      if(combination == null) throw new Exception("No component context has been set");

      try
      {
         return Tessaging.Hosting.Testing.PluggableComponents.FromEnums(combination.Components);
      }
      catch(Exception ex)
      {
         if(throwOnFailure)
            throw new Exception(
               $"""
                The current test does not appear to be a pluggable components test, 
                could not parse pluggable components from the string {combination.Components.select(it => it.ToString())}
                """,
               ex);
         return null;
      }
   }
}
