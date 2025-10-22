using System.Collections.Concurrent;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

/// <summary>
/// Reads pluggable component combinations from configuration file in the assembly directory.
/// 
/// Configuration file format:
/// - Lines starting with "//" are comments and are ignored
/// - Lines starting with "#" are skipped permutations (can be used to document excluded combinations)
/// - Use "*" as a wildcard to expand all enum values for that component position
/// - Each line should have the same number of colon-separated values as there are component types
/// 
/// Examples:
/// - "Microsoft:MsSql:SimpleInjector" - A specific combination
/// - "Microsoft:*:*" - Expands to all combinations of Microsoft with all SqlLayers and all DIContainers  
/// - "*:MsSql:*" - Expands to all combinations of all Serializers with MsSql and all DIContainers
/// - "*:*:*" - Expands to all possible combinations (cartesian product of all component enum values)
/// - "#Newtonsoft:MySql:*" - Documents a skipped combination pattern
/// </summary>
static class ComponentPermutationsConfigurationFileReader
{
   static readonly ConcurrentDictionary<string, IReadOnlyList<ComponentsPermutation>> PermutationsCache = new();

   /// <summary>Gets permutations parsed with the provided component types as enums from the specified configuration file.</summary>
   public static IReadOnlyList<ComponentsPermutation> GetPermutations(string configurationFileName, Type[] componentEnumTypes)
   {
      return PermutationsCache.GetOrAdd(
         configurationFileName,
         fileName => ReadFile(componentEnumTypes, fileName));
   }

   static IReadOnlyList<ComponentsPermutation> ReadFile(Type[] componentEnumTypes, string fileName) =>
      ParseFileContent(ReadFileLines(fileName), componentEnumTypes);

   static string[] ReadFileLines(string fileName)
   {
      var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
      if(!File.Exists(filePath)) throw new Exception($"File does not exist: {filePath}");
      var fileContent = File.ReadAllLines(filePath);
      return fileContent;
   }

   const string Comment = "//";
   const char SkipPermutation = '#';
   const string Wildcard = "*";

   static IReadOnlyList<ComponentsPermutation> ParseFileContent(IReadOnlyList<string> fileLines, Type[] componentTypes)
   {
      var lines = fileLines
                 .Select(it => it.Trim())
                 .Where(it => !string.IsNullOrEmpty(it))
                 .Where(it => !it.StartsWith(Comment))
                 .ToList();

      var activeLines = lines
                       .Where(it => !it.StartsWith(SkipPermutation))
                       .Select(it => it.Split(ComponentsPermutation.Separator))
                       .ToList();

      var allLines = lines
                    .Select(it => it.TrimStart(SkipPermutation))
                    .Select(it => it.Split(ComponentsPermutation.Separator))
                    .ToList();

      if(activeLines.Count == 0)
         return new List<ComponentsPermutation>([]);

      // Validate all lines have the correct number of components
      var invalidLines = activeLines
         .Select((line, index) => new { line, index, lineNumber = index + 1 })
         .Where(x => x.line.Length != componentTypes.Length)
         .ToList();

      if(invalidLines.Any())
      {
         var expectedCount = componentTypes.Length;
         var expectedTypes = string.Join(", ", componentTypes.Select(t => t.Name));
         var errors = string.Join(Environment.NewLine, 
            invalidLines.Select(x => 
               $"  Line {x.lineNumber}: Found {x.line.Length} components [{string.Join(":", x.line)}], expected {expectedCount}"));
         
         throw new InvalidOperationException(
            $"Configuration file has component count mismatches.{Environment.NewLine}" +
            $"Expected {expectedCount} component types: {expectedTypes}{Environment.NewLine}" +
            $"Invalid lines:{Environment.NewLine}{errors}");
      }

      // Expand wildcards and create permutations
      var expandedPermutations = activeLines
         .SelectMany(line => ExpandWildcards(line, componentTypes))
         .ToList();

      return new List<ComponentsPermutation>(
         expandedPermutations.Select(arr => ComponentsPermutation.FromComponentNamesArray(arr, componentTypes))
                             .ToList());
   }

   static IEnumerable<string[]> ExpandWildcards(string[] componentValues, Type[] componentTypes)
   {
      // Find all positions with wildcards
      var wildcardPositions = componentValues
         .Select((value, index) => new { value, index })
         .Where(x => x.value == Wildcard)
         .Select(x => x.index)
         .ToList();

      if(wildcardPositions.Count == 0)
      {
         // No wildcards, return as-is
         yield return componentValues;
         yield break;
      }

      // Get all possible values for each wildcard position
      var wildcardOptions = wildcardPositions
         .Select(pos => Enum.GetNames(componentTypes[pos]))
         .ToList();

      // Generate all combinations using cross product
      foreach(var combination in CrossProduct(wildcardOptions))
      {
         var result = (string[])componentValues.Clone();
         for(int i = 0; i < wildcardPositions.Count; i++)
         {
            result[wildcardPositions[i]] = combination[i];
         }
         yield return result;
      }
   }

   static IEnumerable<List<T>> CrossProduct<T>(List<T[]> lists)
   {
      if(lists.Count == 0)
      {
         yield return new List<T>();
         yield break;
      }

      var firstList = lists[0];
      var remainingLists = lists.Skip(1).ToList();

      foreach(var item in firstList)
      {
         if(remainingLists.Count == 0)
         {
            yield return new List<T> { item };
         }
         else
         {
            foreach(var combination in CrossProduct(remainingLists))
            {
               var result = new List<T> { item };
               result.AddRange(combination);
               yield return result;
            }
         }
      }
   }
}
