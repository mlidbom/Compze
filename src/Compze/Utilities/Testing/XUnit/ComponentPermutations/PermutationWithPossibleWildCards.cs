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

   public IReadOnlyList<ComponentsPermutation> ExpandWildcardsIntoConcretePermutations()
   {
      if(!_wildCardComponents.Any())
      {
         return
         [
            ComponentsPermutation.FromComponentEnumValues(_componentNamesOrWildCards
                                                         .Zip(_componentTypes, (name, type) => (Enum)Enum.Parse(type, name))
                                                         .ToList())
         ];
      }

      var enumValuesForWildCardComponents = _wildCardComponents
                                           .Select(it => it.AllComponents)
                                           .ToList();

      var wildCardComponentsPermutations = ExpandWildCardsIntoPermutationsOfTheWildCardComponents(enumValuesForWildCardComponents);

      return wildCardComponentsPermutations.Select(CreateConcretePermutation).ToList();
   }

   ComponentsPermutation CreateConcretePermutation(WildCardComponentsPermutation replacementValues)
   {
      var concretePermutationEnumValues = new Enum[_componentNamesOrWildCards.Count];

      // First, convert all non-wildcard values to enums
      for(int i = 0; i < _componentNamesOrWildCards.Count; i++)
      {
         var componentNameOrWildcard = _componentNamesOrWildCards[i];
         if(componentNameOrWildcard != Wildcard)
         {
            concretePermutationEnumValues[i] = (Enum)Enum.Parse(_componentTypes[i], componentNameOrWildcard);
         }
      }

      // Then replace wildcards with the actual enum values
      for(int currentWildcardPosition = 0; currentWildcardPosition < _wildCardComponents.Count; currentWildcardPosition++)
      {
         var positionInLine = _wildCardComponents[currentWildcardPosition].Index;
         var replacementValue = replacementValues.Components[currentWildcardPosition];
         concretePermutationEnumValues[positionInLine] = replacementValue;
      }

      return ComponentsPermutation.FromComponentEnumValues(concretePermutationEnumValues);
   }

   static IReadOnlyList<WildCardComponentsPermutation> ExpandWildCardsIntoPermutationsOfTheWildCardComponents(IReadOnlyList<WildCardComponentValues> wildCardComponentValues)
   {
      var cartesianProduct = wildCardComponentValues.Select(it => it.Values).ToList().GenerateCartesianProduct();

      return cartesianProduct.Select(permutation => new WildCardComponentsPermutation(permutation)).ToList();
   }

   readonly record struct WildcardComponent(Type ComponentType, int Index)
   {
      public WildCardComponentValues AllComponents => new(ComponentType, Enum.GetValues(ComponentType).Cast<Enum>().ToReadOnlyList());
   }

   readonly record struct WildCardComponentValues(Type EnumType, IReadOnlyList<Enum> Values);
   readonly record struct WildCardComponentsPermutation(IReadOnlyList<Enum> Components);
}
