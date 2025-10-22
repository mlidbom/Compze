using System.Collections;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

class ComponentsPermutationsList : IEnumerable<ComponentsPermutation>
{
   readonly IReadOnlyList<ComponentsPermutation> Permutations;

   ComponentsPermutationsList(IReadOnlyList<ComponentsPermutation> permutations) => Permutations = permutations;

   internal static ComponentsPermutationsList FromFileContent(string[] rows, Type[] componentEnumTypes)
   {
      var lines = rows
                 .Select(it => it.Trim())
                 .Where(line => !string.IsNullOrEmpty(line))
                 .Where(line => !line.StartsWith("//"))
                 .ToList();

      var activeLines = lines
                       .Where(line => !line.StartsWith('#'))
                       .Select(it => it.Split(ComponentsPermutation.Separator))
                       .ToList();

      var allLines = lines
                    .Select(line => line.TrimStart('#'))
                    .Select(it => it.Split(ComponentsPermutation.Separator))
                    .ToList();

      if(activeLines.Count == 0)
         throw new Exception("Found no active components");

      var componentDimensions = activeLines[0].Length;
      if(activeLines.Any(it => it.Length != componentDimensions))
         throw new Exception("Different lines in the file have different number of components");

      var allComponents = allLines
                         .SelectMany(components => components)
                         .ToHashSet();

      if(componentEnumTypes == null || componentEnumTypes.Length == 0)
         throw new ArgumentException("Component enum types must be provided. Use TypedPCT attribute.", nameof(componentEnumTypes));

      return new ComponentsPermutationsList(
         activeLines.Select(arr => ComponentsPermutation.FromComponentNamesArray(arr, componentEnumTypes)).ToList());
   }

   public IEnumerator<ComponentsPermutation> GetEnumerator() => Permutations.GetEnumerator();
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
