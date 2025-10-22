using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

class ConfigFileLine
{
   const string Wildcard = "*";
   readonly IReadOnlyList<Type> _componentTypes;
   readonly IReadOnlyList<string> _componentNamesOrWildCards;
   readonly IReadOnlyList<WildcardComponent> _wildCardComponents;

   public ConfigFileLine(IReadOnlyList<Type> componentTypes, IReadOnlyList<string> componentNamesOrWildCards)
   {
      _componentTypes = componentTypes;
      _componentNamesOrWildCards = componentNamesOrWildCards;
      _wildCardComponents = _componentNamesOrWildCards
                           .Zip(_componentTypes, (componentName, componentType) => new { componentName, componentType })
                           .Where(it => it.componentName == Wildcard)
                           .Select(it => new WildcardComponent(it.componentType))
                           .ToList();
   }

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

      var wildCardComponentsPermutations = _wildCardComponents
                                          .Select(it => it.AllComponents)
                                          .Select(it => it.Values)
                                          .CartesianProduct()
                                          .Select(it => new WildCardComponentsPermutation(it))
                                          .ToList();

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

   readonly record struct WildcardComponent(Type ComponentType)
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
