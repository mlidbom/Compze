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
                           .Select((componentName, componentIndex) => new {  componentName, componentIndex,  componentType = _componentTypes[componentIndex] })
                           .Where(it => it.componentName == Wildcard)
                           .Select(it => new WildcardComponent(it.componentType, it.componentIndex))
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

   ComponentsPermutation CreateConcretePermutation(WildCardComponentsPermutation wildCardReplacementValues)
   {
      var concreteComponents = _componentNamesOrWildCards
                              .Select((componentNameOrWildcard, componentIndex) =>
                                         componentNameOrWildcard == Wildcard
                                            ? wildCardReplacementValues.ComponentForComponentType(_componentTypes[componentIndex])
                                            : ComponentValue(componentIndex, componentNameOrWildcard))
                              .ToList();

      return ComponentsPermutation.FromComponentEnumValues(concreteComponents);
   }

   Enum ComponentValue(int componentTypeIndex, string componentName) => (Enum)Enum.Parse(_componentTypes[componentTypeIndex], componentName);

   static IReadOnlyList<WildCardComponentsPermutation> ExpandWildCardsIntoPermutationsOfTheWildCardComponents(IReadOnlyList<WildCardComponentValues> wildCardComponentValues) =>
      wildCardComponentValues.Select(it => it.Values)
                             .CartesianProduct()
                             .Select(it => new WildCardComponentsPermutation(it))
                             .ToList();

   readonly record struct WildcardComponent(Type ComponentType, int Index)
   {
      public WildCardComponentValues AllComponents => new(ComponentType, Enum.GetValues(ComponentType).Cast<Enum>().ToReadOnlyList());
   }

   readonly record struct WildCardComponentValues(Type EnumType, IReadOnlyList<Enum> Values);

   class WildCardComponentsPermutation(IReadOnlyList<Enum> components)
   {
      readonly IReadOnlyList<Enum> _components = components;

      public Enum ComponentForComponentType(Type componentType) => _components.Single(it => it.GetType() == componentType);
   }
}
