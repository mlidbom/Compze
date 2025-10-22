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
                                .Select(lineArray => new PermutationWithPossibleWildCards(lineArray))
                                .SelectMany(line => ExpandLineWildcards(line, componentTypes))
                                .ToList();

      return new List<ComponentsPermutation>(
         expandedPermutations.Select(it => ComponentsPermutation.FromComponentNamesList(it.ComponentNamesOrWildCards, componentTypes))
                             .OrderBy(it => it.ToString())
                             .DistinctBy(it => it.ToString())
                             .ToList());
   }

   const string Wildcard = "*";

   readonly record struct PermutationWithPossibleWildCards(IReadOnlyList<string> ComponentNamesOrWildCards);
   readonly record struct WildcardPosition(int Index);
   readonly record struct EnumValues(Type EnumType, IReadOnlyList<Enum> Values);
   readonly record struct ConcretePermutationNoWildCards(IReadOnlyList<string> EnumValueNames);

   static IEnumerable<PermutationWithPossibleWildCards> ExpandLineWildcards(PermutationWithPossibleWildCards line, Type[] componentTypes)
   {
      var wildcardPositions = FindWildcardPositions(line);

      if(wildcardPositions.Count == 0)
      {
         yield return line;
         yield break;
      }

      var allPossibleValuesForEachWildcardPosition = GetAllEnumValuesForWildcardPositions(wildcardPositions, componentTypes);

      var allCombinationsOfWildcardValues = GenerateAllCombinations(allPossibleValuesForEachWildcardPosition);

      foreach(var wildcardValueCombination in allCombinationsOfWildcardValues)
      {
         var expandedLine = ReplaceWildcardsWithValues(line, wildcardPositions, wildcardValueCombination);
         yield return expandedLine;
      }
   }

   static IReadOnlyList<WildcardPosition> FindWildcardPositions(PermutationWithPossibleWildCards line)
   {
      return line.ComponentNamesOrWildCards
                 .Select((value, index) => new { value, index })
                 .Where(x => x.value == Wildcard)
                 .Select(x => new WildcardPosition(x.index))
                 .ToList();
   }

   static IReadOnlyList<EnumValues> GetAllEnumValuesForWildcardPositions(
      IReadOnlyList<WildcardPosition> wildcardPositions,
      Type[] componentTypes)
   {
      return wildcardPositions
            .Select(wildcardPosition => new EnumValues(componentTypes[wildcardPosition.Index], Enum.GetValues(componentTypes[wildcardPosition.Index]).Cast<Enum>().ToReadOnlyList()))
            .ToList();
   }

   static PermutationWithPossibleWildCards ReplaceWildcardsWithValues(
      PermutationWithPossibleWildCards originalLine,
      IReadOnlyList<WildcardPosition> wildcardPositions,
      ConcretePermutationNoWildCards replacementValues)
   {
      var result = originalLine.ComponentNamesOrWildCards.ToList();

      for(int i = 0; i < wildcardPositions.Count; i++)
      {
         var positionInLine = wildcardPositions[i].Index;
         var replacementValue = replacementValues.EnumValueNames[i];
         result[positionInLine] = replacementValue;
      }

      return new PermutationWithPossibleWildCards(result);
   }

   static IEnumerable<ConcretePermutationNoWildCards> GenerateAllCombinations(IReadOnlyList<EnumValues> listsOfPossibleValues)
   {
      if(listsOfPossibleValues.Count == 0)
      {
         yield return new ConcretePermutationNoWildCards([]);
         yield break;
      }

      var firstListOfPossibleValues = listsOfPossibleValues[0];
      var remainingListsOfPossibleValues = listsOfPossibleValues.Skip(1).ToList();

      foreach(var enumValue in firstListOfPossibleValues.Values)
      {
         if(remainingListsOfPossibleValues.Count == 0)
         {
            yield return new ConcretePermutationNoWildCards([enumValue.ToString()]);
         } else
         {
            foreach(var combinationOfRemainingValues in GenerateAllCombinations(remainingListsOfPossibleValues))
            {
               var completeCombination = new List<string> { enumValue.ToString() };
               completeCombination.AddRange(combinationOfRemainingValues.EnumValueNames);
               yield return new ConcretePermutationNoWildCards(completeCombination);
            }
         }
      }
   }
}
