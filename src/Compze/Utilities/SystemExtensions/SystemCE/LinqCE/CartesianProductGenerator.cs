using System.Collections.Generic;
using System.Linq;

namespace Compze.Utilities.SystemCE.LinqCE;

static class CartesianProductGenerator
{
   /// <summary>
   /// Generates the cartesian product (all possible combinations) of multiple lists.
   /// Example: [[A,B], [X,Y]] → [[A,X], [A,Y], [B,X], [B,Y]]
   /// </summary>
   internal static IReadOnlyList<IReadOnlyList<T>> CartesianProduct<T>(this IEnumerable<IReadOnlyList<T>> enumerable)
   {
      var lists = (enumerable as IReadOnlyList<IReadOnlyList<T>>) ?? enumerable.ToList();
      if(lists.Count == 0)
      {
         return [[]];
      }

      // Start with first list as single-element combinations: [[A], [B], [C]]
      var seedListOfLists = lists[0].Select(it => new List<T> { it }.ToReadOnlyList()).ToList();

      // For each remaining list, expand all existing combinations by appending each item from that list
      // This builds up the cartesian product incrementally
      return lists.Skip(1)
                  .Aggregate(seedListOfLists,
                             (accumulator, currentList) =>
                                accumulator.SelectMany(existingCombination =>
                                                          currentList.Select(newValue => existingCombination
                                                                                    .Concat([newValue])
                                                                                    .ToReadOnlyList()))
                                           .ToList())
                  .ToList();
   }
}
