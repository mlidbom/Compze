using System.Collections.Generic;

namespace Compze.Utilities.SystemCE.ThreadingCE.Utilities;

static class EnumerableCE
{
   /// <summary>
   /// generates a sequence of integers beginning with <paramref name="me"/> where each element is
   /// the previous element plus one that includes the upper bound <paramref name="guard"/>
   /// </summary>
   public static IEnumerable<int> Through(this int me, int guard)
   {
      while(me <= guard)
      {
         yield return me++;
      }
   }
}
