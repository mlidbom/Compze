using System.Collections;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

class ComponentsPermutationsList : IEnumerable<ComponentsPermutation>
{
   internal ComponentsPermutationsList(IReadOnlyList<ComponentsPermutation> permutations) => Permutations = permutations;
   readonly IReadOnlyList<ComponentsPermutation> Permutations;


   public IEnumerator<ComponentsPermutation> GetEnumerator() => Permutations.GetEnumerator();
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
