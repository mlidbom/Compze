using System;
using System.Collections.Generic;

// ReSharper disable MemberCanBePrivate.Global

namespace Compze.SystemCE.LinqCE;

/// <summary/>
public static partial class EnumerableCE
{
   ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
   public static IEnumerable<Type> OfTypes<T1>() => Create(typeof(T1));

   ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
   public static IEnumerable<Type> OfTypes<T1, T2>() => OfTypes<T1>().Append(typeof(T2));

   ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
   public static IEnumerable<Type> OfTypes<T1, T2, T3>() => OfTypes<T1, T2>().Append(typeof(T3));

   ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
   public static IEnumerable<Type> OfTypes<T1, T2, T3, T4>() => OfTypes<T1, T2, T3>().Append(typeof(T4));

   ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
   public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5>() => OfTypes<T1, T2, T3, T4>().Append(typeof(T5));

   ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
   public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6>() => OfTypes<T1, T2, T3, T4, T5>().Append(typeof(T6));

   ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
   public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6, T7>() => OfTypes<T1, T2, T3, T4, T5, T6>().Append(typeof(T7));

   ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
   internal static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6, T7, T8>() => OfTypes<T1, T2, T3, T4, T5, T6, T7>().Append(typeof(T8));

   ///<summary>Returns a sequence of types matching the supplied type arguments</summary>
   public static IEnumerable<Type> OfTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>() => OfTypes<T1, T2, T3, T4, T5, T6, T7, T8>().Append(typeof(T9));
}