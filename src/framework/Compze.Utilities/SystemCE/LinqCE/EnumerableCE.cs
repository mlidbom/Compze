using System;
using System.Collections.Generic;
using System.Linq;
using static Compze.Contracts.Assert;

namespace Compze.SystemCE.LinqCE;

/// <summary/>
public static partial class EnumerableCE
{
   /// <summary>
   /// Creates an enumerable consisting of the passed parameter values is order.
   /// </summary>
   /// <typeparam name="T"></typeparam>
   /// <param name="values"></param>
   /// <returns></returns>
   public static IEnumerable<T> Create<T>(params T[] values)
   {
      Argument.NotNull(values);
      return values;
   }

   /// <summary>
   /// Adds <paramref name="instances"/> to the end of <paramref name="source"/>
   /// </summary>
   static IEnumerable<T> Append<T>(this IEnumerable<T> source, params T[] instances)
   {
      Argument.NotNull(source).NotNull(instances);
      return source.Concat(instances);
   }

   //Add these so that we don't waste effort enumerating these types to check if any entries exist.
   internal static bool None<T>(this List<T> me) => me.Count == 0;
   internal static bool None<T>(this IReadOnlyList<T> me) => me.Count == 0;
   internal static bool None<T>(this T[] me) => me.Length == 0;

   /// <summary>
   /// <para>The inversion of Enumerable.Any() .</para>
   /// <para>Returns true if <paramref name="me"/> contains no elements.</para>
   /// </summary>
   /// <returns>true if <paramref name="me"/> contains no objects. Otherwise false.</returns>
   internal static bool None<T>(this IEnumerable<T> me, Func<T,bool> condition)
   {
      Argument.NotNull(me).NotNull(condition);

      return !me.Any(condition);
   }

   /// <summary>
   /// Chops an IEnumerable up into <paramref name="size"/> sized chunks.
   /// </summary>
   internal static IEnumerable<IEnumerable<T>> ChopIntoSizesOf<T>(this IEnumerable<T> me, int size)
   {
      Argument.NotNull(me);

      // ReSharper disable once GenericEnumeratorNotDisposed ReSharper is plain wrong again.
      using var enumerator = me.GetEnumerator();
      var yielded = size;
      while(yielded == size)
      {
         yielded = 0;
         var next = new T[size];
         while(yielded < size && enumerator.MoveNext())
         {
            next[yielded++] = enumerator.Current;
         }

         if(yielded == 0)
         {
            yield break;
         }

         yield return yielded == size ? next : next.Take(yielded);
      }
   }


   /// <summary>
   /// Acting on an <see cref="IEnumerable{T}"/> <paramref name="me"/> where T is an <see cref="IEnumerable{TChild}"/>
   /// returns an <see cref="IEnumerable{TChild}"/> aggregating all the TChild instances
   /// 
   /// Using SelectMany(x=>x) is ugly and unintuitive.
   /// This method provides an intuitively named alternative.
   /// </summary>
   /// <typeparam name="T">A type implementing <see cref="IEnumerable{TChild}"/></typeparam>
   /// <typeparam name="TChild">The type contained in the nested enumerables.</typeparam>
   /// <param name="me">the collection to act upon</param>
   /// <returns>All the objects in all the nested collections </returns>
   internal static IEnumerable<TChild> Flatten<T, TChild>(this IEnumerable<T> me) where T : IEnumerable<TChild>
   {
      Argument.NotNull(me);

      return me.SelectMany(obj => obj);
   }
}