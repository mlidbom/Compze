using Compze.Utilities.SystemCE;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

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

   /// <summary>
   /// Gets all unique component names from all permutations (including ignored ones).
   /// This is used for validation to ensure excluded components actually exist.
   /// </summary>
   public static IReadOnlySet<string> Components => Permutations.AllComponents;
}
