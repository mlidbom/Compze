using System.Collections;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

class ComponentsPermutationsList : IEnumerable<ComponentsPermutation>
{
   const string Comment = "//";
   const char ComponentExclusion = '#';
   readonly IReadOnlyList<ComponentsPermutation> Permutations;

   ComponentsPermutationsList(IReadOnlyList<ComponentsPermutation> permutations) => Permutations = permutations;

   internal static ComponentsPermutationsList FromFileContent(string[] rows, Type[] componentTypes)
   {
      var lines = rows
                 .Select(it => it.Trim())
                 .Where(it => !string.IsNullOrEmpty(it))
                 .Where(it => !it.StartsWith(Comment))
                 .ToList();

      var activeLines = lines
                       .Where(it => !it.StartsWith(ComponentExclusion))
                       .Select(it => it.Split(ComponentsPermutation.Separator))
                       .ToList();

      var allLines = lines
                    .Select(it => it.TrimStart(ComponentExclusion))
                    .Select(it => it.Split(ComponentsPermutation.Separator))
                    .ToList();

      if(activeLines.Count == 0)
         return new ComponentsPermutationsList([]);

      var componentDimensions = activeLines[0].Length;
      if(activeLines.Any(it => it.Length != componentDimensions))
         throw new Exception("Different lines in the file have different number of components");

      return new ComponentsPermutationsList(
         activeLines.Select(arr => ComponentsPermutation.FromComponentNamesArray(arr, componentTypes))
                    .ToList());
   }

   public IEnumerator<ComponentsPermutation> GetEnumerator() => Permutations.GetEnumerator();
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
