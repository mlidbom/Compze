using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.Functional;

///<summary>
/// Methods useful for any type when used in a Linq context
///</summary>
public static class ObjectCE
{
   /// <summary>
   /// Returns <paramref name="me"/> repeated <paramref name="times"/> times.
   /// </summary>
   internal static IEnumerable<T> Repeat<T>(this T me, int times)
   {
      while(times-- > 0)
      {
         yield return me;
      }
   }

   public static string ToStringNotNull(this object @this) => Contract.ReturnNotNull(@this.ToString());
}
