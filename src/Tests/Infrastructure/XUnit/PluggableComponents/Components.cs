using System;
using System.Collections.Generic;
using System.Linq;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

class ComponentsPermutationsList
{
   IReadOnlyList<ComponentsPermutation> Permutations;
   public ComponentsPermutationsList(IReadOnlyList<ComponentsPermutation> permutations) => Permutations = permutations;

   internal static ComponentsPermutationsList ReadFileContent(string[] rows)
   {
      var components = rows
                      .Select(it => it.Trim())
                      .Where(line => !string.IsNullOrEmpty(line))
                      .Where(line => !line.StartsWith('#'))
                      .Select(it => it.Split(ComponentsPermutation.Separator))
                      .ToList();
      if(components.Count == 0)
         throw new Exception("Found no components");

      var componentDimensions = components[0].Length;
      if(components.Any(it => it.Length != componentDimensions))
         throw new Exception("Different lines in the file have different number of components");

      return new ComponentsPermutationsList(components.Select(ComponentsPermutation.FromArray).ToList());
   }
}

class ComponentsPermutation
{
   internal const string Separator = ":";

   public override string ToString() => string.Join(Separator, Components);

   readonly IReadOnlyList<string> Components;
   ComponentsPermutation(IReadOnlyList<string> components) => Components = components;

   internal static ComponentsPermutation FromArray(string[] value) =>
      new(value);

   internal static ComponentsPermutation Parse(string value) =>
      new(value.Split(Separator));
}
