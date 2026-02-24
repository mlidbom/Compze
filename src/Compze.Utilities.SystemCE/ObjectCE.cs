using System.Collections.Generic;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Utilities.SystemCE;

public static class ObjectCE
{
   ///<summary>Returns string.Empty if ToString() returns null.</summary>
   public static string ToStringCE(this object @this) => @this.ToString() ?? string.Empty;

   /// <summary>
   /// Returns <paramref name="me"/> repeated <paramref name="times"/> times.
   /// </summary>
   public static IEnumerable<T> Repeat<T>(this T me, int times)
   {
      while(times-- > 0)
      {
         yield return me;
      }
   }

   public static string ToStringNotNull(this object @this) => ReturnValue.ReturnNotNull(@this.ToString());
}
