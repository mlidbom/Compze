using System.Collections;

namespace Compze.Utilities.Testing.XUnit.v3.ComponentPermutations;

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
