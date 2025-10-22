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
         expandedPermutations.Select(it => ComponentsPermutation.FromComponentNamesList(it.ComponentNames, componentTypes))
                             .OrderBy(it => it.ToString())
                             .DistinctBy(it => it.ToString())
                             .ToList());
   }

   const string Wildcard = "*";

   readonly record struct PermutationWithPossibleWildCards(IReadOnlyList<string> ComponentNamesOrWildCards);
   readonly record struct ConcretePermutationNoWildcards(IReadOnlyList<string> ComponentNames);
   readonly record struct WildcardComponent(Type ComponentType, int Index);
   readonly record struct ExpandedWildCardComponentValues(Type EnumType, IReadOnlyList<Enum> Values);
   readonly record struct WildCardComponentsPermutation(IReadOnlyList<string> ComponentNames);

   static IEnumerable<ConcretePermutationNoWildcards> ExpandLineWildcards(PermutationWithPossibleWildCards line, Type[] componentTypes)
   {
      var wildcardPositions = FindWildcardComponentTypes(line, componentTypes);

      if(wildcardPositions.Count == 0)
      {
         yield return new ConcretePermutationNoWildcards(line.ComponentNamesOrWildCards);
         yield break;
      }

      var enumValuesForWildCardComponents = GetEnumValuesForWildcardComponents(wildcardPositions, componentTypes);

      var expandedPermutations = ExpandWildCardsIntoConcretePermutations(enumValuesForWildCardComponents);

      foreach(var permutation in expandedPermutations)
      {
         yield return CloneLineToCreateConcretePermutation(line, wildcardPositions, permutation);
      }
   }

   static IReadOnlyList<WildcardComponent> FindWildcardComponentTypes(PermutationWithPossibleWildCards wildCardPermutation, Type[] componentTypes)
   {
      return wildCardPermutation.ComponentNamesOrWildCards
                                .Select((value, index) => new { value, index })
                                .Where(x => x.value == Wildcard)
                                .Select(x => new WildcardComponent(componentTypes[x.index], x.index))
                                .ToList();
   }

   static IReadOnlyList<ExpandedWildCardComponentValues> GetEnumValuesForWildcardComponents(
      IReadOnlyList<WildcardComponent> wildcardComponent,
      Type[] componentTypes)
   {
      return wildcardComponent
            .Select(it => new ExpandedWildCardComponentValues(it.ComponentType, Enum.GetValues(it.ComponentType).Cast<Enum>().ToReadOnlyList()))
            .ToList();
   }

   static ConcretePermutationNoWildcards CloneLineToCreateConcretePermutation(
      PermutationWithPossibleWildCards originalLine,
      IReadOnlyList<WildcardComponent> wildcardPositions,
      WildCardComponentsPermutation replacementValues)
   {
      var concretePermutation = originalLine.ComponentNamesOrWildCards.ToList();

      for(int currentWildcardPosition = 0; currentWildcardPosition < wildcardPositions.Count; currentWildcardPosition++)
      {
         var positionInLine = wildcardPositions[currentWildcardPosition].Index;
         var replacementValue = replacementValues.ComponentNames[currentWildcardPosition];
         concretePermutation[positionInLine] = replacementValue;
      }

      return new ConcretePermutationNoWildcards(concretePermutation);
   }

   static IEnumerable<WildCardComponentsPermutation> ExpandWildCardsIntoConcretePermutations(IReadOnlyList<ExpandedWildCardComponentValues> wildCardComponentValues)
   {
      if(wildCardComponentValues.Count == 0)
      {
         yield return new WildCardComponentsPermutation([]);
         yield break;
      }

      var firstComponentTypeValues = wildCardComponentValues[0];
      var otherComponentTypeValues = wildCardComponentValues.Skip(1).ToList();

      foreach(var enumValue in firstComponentTypeValues.Values)
      {
         if(otherComponentTypeValues.Count == 0)
         {
            yield return new WildCardComponentsPermutation([enumValue.ToString()]);
         } else
         {
            foreach(var combinationOfRemainingValues in ExpandWildCardsIntoConcretePermutations(otherComponentTypeValues))
            {
               var completeCombination = new List<string> { enumValue.ToString() };
               completeCombination.AddRange(combinationOfRemainingValues.ComponentNames);
               yield return new WildCardComponentsPermutation(completeCombination);
            }
         }
      }
   }
}
