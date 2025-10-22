using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

class ConfigFileLine(IReadOnlyList<Type> componentTypes, IReadOnlyList<string> componentNamesOrWildCards)
{
   const string Wildcard = "*";
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
