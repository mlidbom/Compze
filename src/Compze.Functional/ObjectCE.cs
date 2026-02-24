using System.Collections.Generic;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Functional;

///<summary>
/// Methods useful for any type when used in a Linq context
///</summary>
public static class ObjectCE
{
   /// <summary>
   /// Returns <paramref name="me"/> repeated <paramref name="times"/> times.
   /// </summary>
   public static IEnumerable<T> _repeat<T>(this T me, int times)
   {
      while(times-- > 0)
      {
         yield return me;
      }
   }

   public static string ToStringNotNull(this object @this) => ReturnValue.ReturnNotNull(@this.ToString());
}
