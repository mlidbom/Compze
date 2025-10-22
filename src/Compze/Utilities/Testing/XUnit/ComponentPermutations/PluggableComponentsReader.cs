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

   /// <summary>
   /// Gets all unique component names from all permutations (including ignored ones).
   /// This is used for validation to ensure excluded components actually exist.
   /// Uses TypedPCT component types to parse the file.
   /// </summary>
   public static IReadOnlySet<string> Components
   {
      get
      {
         // Read file content and extract component names without parsing as enums
         var lines = FileContentLazy.Value
                    .Select(it => it.Trim())
                    .Where(line => !string.IsNullOrEmpty(line))
                    .Where(line => !line.StartsWith("//")) // Comments
                    .Select(line => line.TrimStart('#')) // Remove # prefix if present
                    .Select(it => it.Split(ComponentsPermutation.Separator))
                    .SelectMany(components => components)
                    .ToHashSet();
         
         return lines;
      }
   }
}
