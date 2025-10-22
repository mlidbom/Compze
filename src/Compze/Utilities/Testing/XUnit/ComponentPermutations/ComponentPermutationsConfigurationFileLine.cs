using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

class ComponentPermutationsConfigurationFileLine
{
   const string Wildcard = "*";
   readonly IReadOnlyList<Type> _componentTypes;
   readonly IReadOnlyList<string> _componentNamesOrWildCards;
   readonly IReadOnlyList<WildcardComponent> _wildCardComponents;

   public ComponentPermutationsConfigurationFileLine(IReadOnlyList<Type> componentTypes, string line)
   {
      _componentTypes = componentTypes;
      _componentNamesOrWildCards = line.Split(ComponentsPermutation.Separator);
      _wildCardComponents = _componentNamesOrWildCards
                           .Zip(_componentTypes, (componentName, componentType) => new { componentName, componentType })
                           .Where(it => it.componentName == Wildcard)
                           .Select(it => new WildcardComponent(it.componentType))
                           .ToList();
   }

   public IReadOnlyList<ComponentsPermutation> ExpandWildcardsIntoConcretePermutations()
   {
      if(!_wildCardComponents.Any())
         return
         [
            ComponentsPermutation.FromComponentEnumValues(_componentNamesOrWildCards
                                                         .Zip(_componentTypes, (name, type) => (Enum)Enum.Parse(type, name))
                                                         .ToList())
         ];

      return _wildCardComponents
            .Select(it => it.AllComponents)
            .CartesianProduct()
            .Select(it => new WildCardComponentsPermutation(it))
            .Select(CreateConcretePermutation)
            .ToList();
   }

   ComponentsPermutation CreateConcretePermutation(WildCardComponentsPermutation wildCardReplacementValues)
   {
      var concreteComponents = _componentNamesOrWildCards
                              .Select((componentName, componentIndex) =>
                                         componentName == Wildcard
                                            ? wildCardReplacementValues.ComponentForComponentType(_componentTypes[componentIndex])
                                            : ComponentValue(componentIndex, componentName))
                              .ToList();

      return ComponentsPermutation.FromComponentEnumValues(concreteComponents);
   }

   Enum ComponentValue(int componentTypeIndex, string componentName) => (Enum)Enum.Parse(_componentTypes[componentTypeIndex], componentName);

   readonly record struct WildcardComponent(Type ComponentType)
   {
      public IReadOnlyList<Enum> AllComponents => Enum.GetValues(ComponentType).Cast<Enum>().ToReadOnlyList();
   }

   class WildCardComponentsPermutation(IReadOnlyList<Enum> components)
   {
      readonly IReadOnlyList<Enum> _components = components;
      public Enum ComponentForComponentType(Type componentType) => _components.Single(predicate: it => it.GetType() == componentType);
   }
}
