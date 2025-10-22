using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

class ConfigFileLine
{
   const string Wildcard = "*";
   readonly IReadOnlyList<Type> _componentTypes;
   readonly IReadOnlyList<string> _componentNamesOrWildCards;

   public ConfigFileLine(IReadOnlyList<Type> componentTypes, IReadOnlyList<string> componentNamesOrWildCards)
   {
      _componentTypes = componentTypes;
      _componentNamesOrWildCards = componentNamesOrWildCards;
      _wildCardComponents = _componentNamesOrWildCards
                           .Select((componentNameOrWildCard, index) => new { value = componentNameOrWildCard, index })
                           .Where(it => it.value == Wildcard)
                           .Select(it => new WildcardComponent(_componentTypes[it.index], it.index))
                           .ToList();
   }

   readonly IReadOnlyList<WildcardComponent> _wildCardComponents;

   public IEnumerable<ComponentsPermutation> ExpandWildcardsIntoConcretePermutations()
   {
      if(_wildCardComponents.Count == 0)
      {
         yield return ComponentsPermutation.FromComponentEnumValues(_componentNamesOrWildCards
                                                                   .Zip(_componentTypes, (name, type) => (Enum)Enum.Parse(type, name))
                                                                   .ToList());
      } else
      {
         var enumValuesForWildCardComponents = _wildCardComponents
                                              .Select(it => it.AllComponents)
                                              .ToList();

         var wildCardComponentsPermutations = ExpandWildCardsIntoPermutationsOfTheWildCardComponents(enumValuesForWildCardComponents);

         foreach(var permutation in wildCardComponentsPermutations)
         {
            yield return CloneLineToCreateConcretePermutation(_wildCardComponents, permutation, _componentTypes);
         }
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
