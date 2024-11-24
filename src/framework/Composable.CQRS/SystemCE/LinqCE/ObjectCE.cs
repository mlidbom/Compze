using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.SystemCE.LinqCE;

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

   public static T Mutate<T>(this T @this, Action<T> mutate)
   {
      mutate(@this);
      return @this;
   }

   public static async Task<T> MutateAsync<T>(this T @this, Func<T, Task> mutate)
   {
      await mutate(@this).NoMarshalling();
      return @this;
   }

   public static TResult MapTo<TValue, TResult>(this TValue @this, Func<TValue, TResult> transform) => transform(@this);

   ///<summary>Sometimes you want to continue a method chain but don't care about the previous value. This drops it</summary>
   public static TResult Then<TValue, TResult>(this TValue _, Func<TResult> func) => func();
   
   ///<summary>Sometimes you want to continue a method chain but don't care about the previous value. This drops it</summary>
   public static TResult Then<TValue, TResult>(this TValue _, TResult value) => value;

   ///<summary>Enables chaining statements that return void. For when separate lines of code is inconvenient, such as in many lambda expressions.</summary>
   internal static VoidCE Then<TValue>(this TValue _, Action action) => VoidCE.From(action);

   public static string ToStringNotNull(this object @this) => Contract.ReturnNotNull(@this.ToString());
}
