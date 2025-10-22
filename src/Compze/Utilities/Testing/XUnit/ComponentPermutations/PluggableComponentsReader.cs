using Compze.Utilities.SystemCE;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

/// <summary>
/// Reads pluggable component combinations from configuration file in the assembly directory.
/// </summary>
static class PluggableComponentsReader
{
   const string TestUsingPluggableComponentCombinations = "TestUsingPluggableComponentCombinations";

   static readonly LazyCE<string[]> FileContentLazy = new(() =>
   {
      var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TestUsingPluggableComponentCombinations);
      if(!File.Exists(filePath)) throw new Exception($"{filePath} is missing");
      return File.ReadAllLines(filePath);
   });

   /// <summary>
   /// Gets permutations parsed with the provided component types as enums.
   /// </summary>
   public static ComponentsPermutationsList GetPermutations(Type[] componentEnumTypes) =>
      ComponentsPermutationsList.FromFileContent(FileContentLazy.Value, componentEnumTypes);
}
