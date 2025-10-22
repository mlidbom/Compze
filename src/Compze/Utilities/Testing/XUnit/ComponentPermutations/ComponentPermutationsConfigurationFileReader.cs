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
                                .Select(componentsOrWildCards => new PermutationWithPossibleWildCards(componentTypes, componentsOrWildCards))
                                .SelectMany(line => line.ExpandWildcardsIntoConcretePermutations())
                                .ToList();

      return new List<ComponentsPermutation>(
         expandedPermutations.OrderBy(it => it.ToString())
                             .DistinctBy(it => it.ToString())
                             .ToList());
   }

   const string Wildcard = "*";

   class PermutationWithPossibleWildCards(IReadOnlyList<Type> componentTypes, IReadOnlyList<string> componentNamesOrWildCards)
   {
      readonly IReadOnlyList<Type> _componentTypes = componentTypes;
      readonly IReadOnlyList<string> _componentNamesOrWildCards = componentNamesOrWildCards;

      IReadOnlyList<WildcardComponent> WildCardComponents =>
         _componentNamesOrWildCards
           .Select((componentNameOrWildCard, index) => new { value = componentNameOrWildCard, index })
           .Where(it => it.value == Wildcard)
           .Select(it => new WildcardComponent(_componentTypes[it.index], it.index))
           .ToList();

      public IEnumerable<ComponentsPermutation> ExpandWildcardsIntoConcretePermutations()
      {
         var wildcardComponents = WildCardComponents;

         if(wildcardComponents.Count == 0)
         {
            var enumValues = _componentNamesOrWildCards
                            .Zip(_componentTypes, (name, type) => (Enum)Enum.Parse(type, name))
                            .ToList();
            yield return ComponentsPermutation.FromComponentEnumValues(enumValues);
            yield break;
         }

         var enumValuesForWildCardComponents = wildcardComponents
                                              .Select(it => it.AllComponents)
                                              .ToList();

         var wildCardComponentsPermutations = ExpandWildCardsIntoPermutationsOfTheWildCardComponents(enumValuesForWildCardComponents);

         foreach(var permutation in wildCardComponentsPermutations)
         {
            yield return CloneLineToCreateConcretePermutation(wildcardComponents, permutation, _componentTypes);
         }
      }

      ComponentsPermutation CloneLineToCreateConcretePermutation(
         IReadOnlyList<WildcardComponent> wildcardComponents,
         WildCardComponentsPermutation replacementValues,
         IReadOnlyList<Type> componentTypes)
      {
         var concretePermutationEnumValues = new Enum[_componentNamesOrWildCards.Count];

         // First, convert all non-wildcard values to enums
         for(int i = 0; i < _componentNamesOrWildCards.Count; i++)
         {
            var componentNameOrWildcard = _componentNamesOrWildCards[i];
            if(componentNameOrWildcard != Wildcard)
            {
               concretePermutationEnumValues[i] = (Enum)Enum.Parse(componentTypes[i], componentNameOrWildcard);
            }
         }

         // Then replace wildcards with the actual enum values
         for(int currentWildcardPosition = 0; currentWildcardPosition < wildcardComponents.Count; currentWildcardPosition++)
         {
            var positionInLine = wildcardComponents[currentWildcardPosition].Index;
            var replacementValue = replacementValues.Components[currentWildcardPosition];
            concretePermutationEnumValues[positionInLine] = replacementValue;
         }

         return ComponentsPermutation.FromComponentEnumValues(concretePermutationEnumValues);
      }

      static IEnumerable<WildCardComponentsPermutation> ExpandWildCardsIntoPermutationsOfTheWildCardComponents(IReadOnlyList<WildCardComponentValues> wildCardComponentValues)
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
               yield return new WildCardComponentsPermutation([enumValue]);
            } else
            {
               foreach(var wildCardComponentsPermutation in ExpandWildCardsIntoPermutationsOfTheWildCardComponents(otherComponentTypeValues))
               {
                  var completeCombination = new List<Enum> { enumValue };
                  completeCombination.AddRange(wildCardComponentsPermutation.Components);
                  yield return new WildCardComponentsPermutation(completeCombination);
               }
            }
         }
      }
      readonly record struct WildcardComponent(Type ComponentType, int Index)
      {
         public WildCardComponentValues AllComponents => new(ComponentType, Enum.GetValues(ComponentType).Cast<Enum>().ToReadOnlyList());
      }

      readonly record struct WildCardComponentValues(Type EnumType, IReadOnlyList<Enum> Values);
      readonly record struct WildCardComponentsPermutation(IReadOnlyList<Enum> Components);
   }
}
