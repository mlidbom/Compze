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
   /// Gets permutations parsed with the provided component types.
   /// Component types must be provided - components are always parsed as enums.
   /// </summary>
   public static ComponentsPermutationsList GetPermutations(Type[] componentEnumTypes)
   {
      if(componentEnumTypes == null || componentEnumTypes.Length == 0)
         throw new ArgumentException("Component enum types must be provided", nameof(componentEnumTypes));
      
      return ComponentsPermutationsList.FromFileContent(FileContentLazy.Value, componentEnumTypes);
   }

   /// <summary>
   /// Gets all unique component names from all permutations (including ignored ones).
   /// This is used for validation to ensure excluded components actually exist.
   /// </summary>
   public static IReadOnlySet<string> Components => 
      ComponentsPermutationsList.FromFileContent(FileContentLazy.Value, null).AllComponents;
}
