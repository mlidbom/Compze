using System.Collections.Generic;
using System.Linq;

namespace Compze.Utilities.SystemCE.LinqCE;

public static class CartesianProductGenerator
{
   /// <summary>
   /// Generates the cartesian product (all possible combinations) of multiple lists.
   /// Example: [[A,B], [X,Y]] → [[A,X], [A,Y], [B,X], [B,Y]]
   /// 
   /// Algorithm explanation:
   /// Think of each combination as a number in a mixed-radix number system where each position has a different base.
   /// For lists [A,B], [X,Y,Z], [1,2]:
   /// - Position 0 has base 2 (A or B)
   /// - Position 1 has base 3 (X, Y, or Z)
   /// - Position 2 has base 2 (1 or 2)
   /// 
   /// We iterate through all combinations (0 to 11 in this case = 2*3*2 combinations).
   /// For combination index 7:
   /// - Divide 7 by products of later bases to get index in each list
   /// - Position 0: 7 mod 2 = 1 → B
   /// - Position 1: (7/2) mod 3 = 1 → Y  
   /// - Position 2: (7/6) mod 2 = 1 → 2
   /// Result: [B, Y, 2]
   /// </summary>
   public static IReadOnlyList<IReadOnlyList<T>> CartesianProduct<T>(this IEnumerable<IReadOnlyList<T>> enumerable)
   {
      var lists = (enumerable as IReadOnlyList<IReadOnlyList<T>>) ?? enumerable.ToList();
      
      if(lists.Count == 0)
         return [[]];

      // Calculate total combinations upfront: product of all list sizes
      var totalCombinations = 1;
      foreach(var list in lists)
      {
         totalCombinations *= list.Count;
      }

      var result = new List<IReadOnlyList<T>>(totalCombinations);

      // Generate each combination by treating the combination index like a mixed-radix number
      // where each position has a different "base" (the size of that list)
      for(int combinationIndex = 0; combinationIndex < totalCombinations; combinationIndex++)
      {
         var combination = new T[lists.Count];
         var remainingIndex = combinationIndex;

         for(int listPosition = lists.Count - 1; listPosition >= 0; listPosition--)
         {
            var currentList = lists[listPosition];
            var indexInCurrentList = remainingIndex % currentList.Count;
            combination[listPosition] = currentList[indexInCurrentList];
            remainingIndex /= currentList.Count;
         }

         result.Add(combination);
      }

      return result;
   }
}
