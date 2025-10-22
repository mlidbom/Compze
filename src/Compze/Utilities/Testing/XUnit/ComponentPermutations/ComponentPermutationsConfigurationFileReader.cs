using System.Collections.Concurrent;
using Compze.Utilities.SystemCE.LinqCE;

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

      // Expand wildcards and create permutations
      var expandedPermutations = activeLines
                                .SelectMany(line => ExpandLineWildcards(line, componentTypes))
                                .ToList();

      return new List<ComponentsPermutation>(
         expandedPermutations.Select(arr => ComponentsPermutation.FromComponentNamesArray(arr, componentTypes))
                             .OrderBy(it => it.ToString())
                             .DistinctBy(it => it.ToString())
                             .ToList());
   }

   const string Wildcard = "*";

   static IEnumerable<string[]> ExpandLineWildcards(string[] componentValues, Type[] componentTypes)
   {
      var wildcardComponentIndexes = componentValues
                                    .Select((value, index) => new { value, index })
                                    .Where(x => x.value == Wildcard)
                                    .Select(x => x.index)
                                    .ToList();

      if(wildcardComponentIndexes.Count == 0)
      {
         yield return componentValues;
         yield break;
      }

      var wildCardComponentsExpandedIntoAllValuesInTheEnums = wildcardComponentIndexes
                                                            .Select(pos => Enum.GetNames(componentTypes[pos]).ToReadOnlyList())
                                                            .ToList();

      // Generate all combinations using cross product
      foreach(var combination in CrossProduct(wildCardComponentsExpandedIntoAllValuesInTheEnums))
      {
         var result = (string[])componentValues.Clone();
         for(int i = 0; i < wildcardComponentIndexes.Count; i++)
         {
            result[wildcardComponentIndexes[i]] = combination[i];
         }

         yield return result;
      }
   }

   static IEnumerable<IReadOnlyList<string>> CrossProduct(IReadOnlyList<IReadOnlyList<string>> lists)
   {
      if(lists.Count == 0)
      {
         yield return new List<string>();
         yield break;
      }

      var firstList = lists[0];
      var remainingLists = lists.Skip(1).ToList();

      foreach(var item in firstList)
      {
         if(remainingLists.Count == 0)
         {
            yield return [item];
         } else
         {
            foreach(var combination in CrossProduct(remainingLists))
            {
               var result = new List<string> { item };
               result.AddRange(combination);
               yield return result;
            }
         }
      }
   }
}
