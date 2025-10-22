using System.Collections;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

class ComponentsPermutationsList : IEnumerable<ComponentsPermutation>
{
   internal readonly IReadOnlyList<ComponentsPermutation> Permutations;
   internal readonly IReadOnlySet<string> AllComponents;

   ComponentsPermutationsList(IReadOnlyList<ComponentsPermutation> permutations, IReadOnlySet<string> allComponents)
   {
      Permutations = permutations;
      AllComponents = allComponents;
   }

   internal ComponentsPermutationsList Exclude(ExclusionsCollection exclusions) =>
      new(Permutations.Where(permutation => !exclusions.Excludes(permutation)).ToList(), AllComponents);

   internal static ComponentsPermutationsList FromFileContent(string[] rows, Type[] componentEnumTypes)
   {
      var lines = rows
                 .Select(it => it.Trim())
                 .Where(line => !string.IsNullOrEmpty(line))
                 .Where(line => !line.StartsWith("//")) // Comments
                 .ToList();

      var activeLines = lines
                       .Where(line => !line.StartsWith('#')) // Ignored/excluded permutations
                       .Select(it => it.Split(ComponentsPermutation.Separator))
                       .ToList();

      var allLines = lines
                    .Select(line => line.TrimStart('#')) // Remove # prefix if present
                    .Select(it => it.Split(ComponentsPermutation.Separator))
                    .ToList();

      if(activeLines.Count == 0)
         throw new Exception("Found no active components");

      var componentDimensions = activeLines[0].Length;
      if(activeLines.Any(it => it.Length != componentDimensions))
         throw new Exception("Different lines in the file have different number of components");

      // Collect all unique components from all lines (active and ignored)
      var allComponents = allLines
                         .SelectMany(components => components)
                         .ToHashSet();

      // Component types must always be provided - we only support typed enums now
      if(componentEnumTypes == null || componentEnumTypes.Length == 0)
         throw new ArgumentException("Component enum types must be provided. Use TypedPCT attribute.", nameof(componentEnumTypes));

      // Parse with types - components are always enums
      return new ComponentsPermutationsList(
         activeLines.Select(arr => ComponentsPermutation.FromArray(arr, componentEnumTypes)).ToList(),
         allComponents);
   }

   public IEnumerator<ComponentsPermutation> GetEnumerator() => Permutations.GetEnumerator();
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
