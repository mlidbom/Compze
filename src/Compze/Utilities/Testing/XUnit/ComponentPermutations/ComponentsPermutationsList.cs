using System.Collections;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

class ComponentsPermutationsList : IEnumerable<ComponentsPermutation>
{
   const string Comment = "//";
   const char ComponentExclusion = '#';
   readonly IReadOnlyList<ComponentsPermutation> Permutations;

   ComponentsPermutationsList(IReadOnlyList<ComponentsPermutation> permutations) => Permutations = permutations;

   internal static ComponentsPermutationsList FromFileContent(string[] rows, Type[] componentEnumTypes)
   {
      var lines = rows
                 .Select(it => it.Trim())
                 .Where(line => !string.IsNullOrEmpty(line))
                 .Where(line => !line.StartsWith(Comment))
                 .ToList();

      var activeLines = lines
                       .Where(line => !line.StartsWith(ComponentExclusion))
                       .Select(it => it.Split(ComponentsPermutation.Separator))
                       .ToList();

      var allLines = lines
                    .Select(line => line.TrimStart(ComponentExclusion))
                    .Select(it => it.Split(ComponentsPermutation.Separator))
                    .ToList();

      if(activeLines.Count == 0)
         return new ComponentsPermutationsList([]);

      var componentDimensions = activeLines[0].Length;
      if(activeLines.Any(it => it.Length != componentDimensions))
         throw new Exception("Different lines in the file have different number of components");

      return new ComponentsPermutationsList(
         activeLines.Select(arr => ComponentsPermutation.FromComponentNamesArray(arr, componentEnumTypes))
                    .ToList());
   }

   public IEnumerator<ComponentsPermutation> GetEnumerator() => Permutations.GetEnumerator();
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
