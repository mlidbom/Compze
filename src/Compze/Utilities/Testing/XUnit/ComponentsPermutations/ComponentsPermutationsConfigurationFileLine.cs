using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Utilities.Testing.XUnit.ComponentsPermutations;

class ComponentsPermutationsConfigurationFileLine
{
   const string Wildcard = "*";
   readonly IReadOnlyList<Type> _componentTypes;
   readonly IReadOnlyList<string> _componentNamesOrWildCards;
   readonly IReadOnlyList<WildcardComponent> _wildCardComponents;

   public ComponentsPermutationsConfigurationFileLine(IReadOnlyList<Type> componentTypes, string line)
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

   ComponentsPermutation CreateConcretePermutation(WildCardComponentsPermutation wildCardReplacementValues) =>
      _componentNamesOrWildCards
        .Select((componentName, componentIndex) =>
                   componentName == Wildcard
                      ? wildCardReplacementValues.ComponentFor(_componentTypes[componentIndex])
                      : ComponentValue(componentIndex, componentName))
        ._(ComponentsPermutation.FromComponentEnumValues);

   Enum ComponentValue(int componentTypeIndex, string componentName) => (Enum)Enum.Parse(_componentTypes[componentTypeIndex], componentName);

   readonly record struct WildcardComponent(Type ComponentType)
   {
      public IReadOnlyList<Enum> AllComponents => Enum.GetValues(ComponentType).Cast<Enum>().ToReadOnlyList();
   }

   class WildCardComponentsPermutation(IReadOnlyList<Enum> components)
   {
      readonly IReadOnlyList<Enum> _components = components;
      public Enum ComponentFor(Type componentType) => _components.Single(predicate: it => it.GetType() == componentType);
   }
}
