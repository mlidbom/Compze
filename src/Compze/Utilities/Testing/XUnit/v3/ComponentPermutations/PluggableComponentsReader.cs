using Compze.Utilities.SystemCE;

namespace Compze.Utilities.Testing.XUnit.v3.ComponentPermutations;

/// <summary>
/// Reads pluggable component combinations from configuration file in the assembly directory.
/// </summary>
static class PluggableComponentsReader
{
   const string TestUsingPluggableComponentCombinations = "TestUsingPluggableComponentCombinations";

   static readonly LazyCE<ComponentsPermutationsList> CombinationsLazy = new(() =>
   {
      var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TestUsingPluggableComponentCombinations);

      if(!File.Exists(filePath)) throw new Exception($"{filePath} is missing");

      return ComponentsPermutationsList.FromFileContent(File.ReadAllLines(filePath));
   });

   public static ComponentsPermutationsList Permutations => CombinationsLazy.Value;
}
