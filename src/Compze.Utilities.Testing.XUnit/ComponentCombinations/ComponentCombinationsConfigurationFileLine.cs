using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Utilities.Testing.XUnit.ComponentCombinations;

public class ComponentCombinationsConfigurationFileLine
{
   const string Wildcard = "*";
   readonly IReadOnlyList<Type> _componentTypes;
   readonly IReadOnlyList<string> _componentNamesOrWildCards;
   readonly IReadOnlyList<WildcardComponent> _wildCardComponents;

   public ComponentCombinationsConfigurationFileLine(IReadOnlyList<Type> componentTypes, string line)
   {
      _componentTypes = componentTypes;
      _componentNamesOrWildCards = line.Split(ComponentCombination.Separator);
      _wildCardComponents = _componentNamesOrWildCards
                           .Zip(_componentTypes, (componentName, componentType) => new { componentName, componentType })
                           .Where(it => it.componentName == Wildcard)
                           .Select(it => new WildcardComponent(it.componentType))
                           .ToList();
   }

   public IReadOnlyList<ComponentCombination> ExpandWildcardsIntoConcretePermutations()
   {
      if(!_wildCardComponents.Any())
         return
         [
            ComponentCombination.FromComponentEnumValues(_componentNamesOrWildCards
                                                         .Zip(_componentTypes, (name, type) => (Enum)Enum.Parse(type, name))
                                                         .ToList())
         ];

      return _wildCardComponents
            .Select(it => it.AllComponents)
            .CartesianProduct()
            .Select(it => new WildCardComponentCombination(it))
            .Select(CreateConcretePermutation)
            .ToList();
   }

   ComponentCombination CreateConcretePermutation(WildCardComponentCombination wildCardReplacementValues) =>
      _componentNamesOrWildCards
        .Select((componentName, componentIndex) =>
                   componentName == Wildcard
                      ? wildCardReplacementValues.ComponentFor(_componentTypes[componentIndex])
                      : ComponentValue(componentIndex, componentName))
        ._(ComponentCombination.FromComponentEnumValues);

   Enum ComponentValue(int componentTypeIndex, string componentName) => (Enum)Enum.Parse(_componentTypes[componentTypeIndex], componentName);

   public readonly record struct WildcardComponent(Type ComponentType)
   {
      public IReadOnlyList<Enum> AllComponents => Enum.GetValues(ComponentType).Cast<Enum>().ToReadOnlyList();
   }

   public class WildCardComponentCombination(IReadOnlyList<Enum> components)
   {
      readonly IReadOnlyList<Enum> _components = components;
      public Enum ComponentFor(Type componentType) => _components.Single(predicate: it => it.GetType() == componentType);
   }
}
