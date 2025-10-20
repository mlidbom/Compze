using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Compze.Tests.Infrastructure.XUnit.PluggableComponents;

class ComponentsPermutationsList : IEnumerable<ComponentsPermutation>
{
   internal readonly IReadOnlyList<ComponentsPermutation> Permutations;
   public ComponentsPermutationsList(IReadOnlyList<ComponentsPermutation> permutations) => Permutations = permutations;

   internal ComponentsPermutationsList Exclude(string[] excluded) =>
      new(Permutations.Where(it => !excluded.Any(it.Components.Contains)).ToList());

   internal static ComponentsPermutationsList FromFileContent(string[] rows)
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

   public IEnumerator<ComponentsPermutation> GetEnumerator() => Permutations.GetEnumerator();
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

class ComponentsPermutation
{
   internal const string Separator = ":";

   public override string ToString() => string.Join(Separator, Components);

   internal readonly IReadOnlyList<string> Components;
   ComponentsPermutation(IReadOnlyList<string> components) => Components = components;

   internal static ComponentsPermutation FromArray(string[] value) =>
      new(value);

   internal static ComponentsPermutation Parse(string value) =>
      new(value.Split(Separator));
}
