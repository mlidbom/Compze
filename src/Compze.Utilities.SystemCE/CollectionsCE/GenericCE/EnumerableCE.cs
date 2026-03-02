using System;
using System.Collections.Generic;
using System.Linq;

namespace Compze.Utilities.SystemCE.CollectionsCE.GenericCE;

public static class EnumerableCE
{
   ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
   public static IEnumerable<Type> OfTypes<T1>() =>
      [typeof(T1)];

   ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
   public static IEnumerable<Type> OfTypes<T1, T2>() => OfTypes<T1>().Append(typeof(T2));

   ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
   public static IEnumerable<Type> OfTypes<T1, T2, T3>() => OfTypes<T1, T2>().Append(typeof(T3));

   ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
   public static IEnumerable<Type> OfTypes<T1, T2, T3, T4>() => OfTypes<T1, T2, T3>().Append(typeof(T4));

   ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
   public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5>() => OfTypes<T1, T2, T3, T4>().Append(typeof(T5));

}
