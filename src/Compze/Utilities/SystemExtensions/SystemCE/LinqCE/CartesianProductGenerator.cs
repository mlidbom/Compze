using System.Collections.Generic;
using System.Linq;

namespace Compze.Utilities.SystemCE.LinqCE;

static class CartesianProductGenerator
{
   internal static IReadOnlyList<IReadOnlyList<T>> CartesianProduct<T>(this IEnumerable<IReadOnlyList<T>> enumerable)
   {
      var lists = (enumerable as IReadOnlyList<IReadOnlyList<T>>) ?? enumerable.ToList();
      if(lists.Count == 0)
      {
         return [[]];
      }

      var seedListOfLists = lists[0].Select(it => new List<T> { it }.ToReadOnlyList()).ToList();

      return lists.Skip(1)
                  .Aggregate(seedListOfLists,
                             (accumulator, current) =>
                                accumulator.SelectMany(existingCombination =>
                                                          current.Select(newValue => existingCombination
                                                                                    .Concat([newValue])
                                                                                    .ToReadOnlyList()))
                                           .ToList())
                  .ToList();
   }
}
