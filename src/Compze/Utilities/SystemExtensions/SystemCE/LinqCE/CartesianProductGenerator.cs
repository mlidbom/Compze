using System.Collections.Generic;
using System.Linq;

namespace Compze.Utilities.SystemCE.LinqCE;

static class CartesianProductGenerator
{
   internal static IReadOnlyList<IReadOnlyList<T>> CartesianProduct<T>(this IEnumerable<IReadOnlyList<T>> enumerable)
   {
      var lists = enumerable.ToList();
      if(lists.Count == 0)
      {
         return [[]];
      }

      // Start with all values from the first list as single-element combinations
      IEnumerable<IReadOnlyList<T>> combinations = lists[0].Select(v => new List<T> { v } as IReadOnlyList<T>);

      // For each remaining list, combine it with all existing combinations
      for(int i = 1; i < lists.Count; i++)
      {
         var currentList = lists[i];
         combinations = combinations.SelectMany(existingCombination =>
                                                   currentList.Select(newValue => existingCombination.Concat([newValue]).ToList() as IReadOnlyList<T>));
      }

      return combinations.ToList();
   }
}
