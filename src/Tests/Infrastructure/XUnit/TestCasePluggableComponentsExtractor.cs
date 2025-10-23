using System;
using Compze.Utilities.Functional;
using Compze.Utilities.Testing.XUnit.ComponentPermutations;

namespace Compze.Tests.Infrastructure.XUnit;

static class TestCasePluggableComponentsExtractor
{
   public static Tessaging.Hosting.Testing.PluggableComponents ToPluggableComponents(this ComponentsPermutation? permutation) =>
      permutation.TryExtractPluggableComponents(throwOnFailure: true)!.Value;

   public static Tessaging.Hosting.Testing.PluggableComponents? TryExtractPluggableComponents(this ComponentsPermutation? permutation,
                                                                                              bool throwOnFailure = false)
   {
      if(permutation == null) throw new Exception("No component context has been set");

      try
      {
         return Tessaging.Hosting.Testing.PluggableComponents.FromEnums(permutation.Components);
      }
      catch(Exception ex)
      {
         if(throwOnFailure)
            throw new Exception(
               $"""
                The current test does not appear to be a pluggable components test, 
                could not parse pluggable components from the string {permutation.Components.select(it => it.ToString())}
                """,
               ex);
         return null;
      }
   }
}
