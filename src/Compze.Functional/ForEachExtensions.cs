using System;
using System.Collections.Generic;
using JetBrains.Annotations;

// ReSharper disable PossibleMultipleEnumeration

namespace Compze.Functional;

public static class ForEachExtensions
{
   extension<TItem>(IEnumerable<TItem> @this)
   {
      /// <summary>Executes <paramref name="action"/> for each element in the collection and returns the original collection.</summary>
      public IEnumerable<TItem> _forEach<TReturn>([InstantHandle] Func<TItem, TReturn> action)
      {
         foreach(var item in @this)
         {
            action(item);
         }

         return @this;
      }

      /// <summary>Executes <paramref name="action"/> for each element in the collection and returns the original collection.</summary>
      public IEnumerable<TItem> _forEach([InstantHandle] Action<TItem> action)
      {
         foreach(var item in @this)
         {
            action(item);
         }

         return @this;
      }

      /// <summary>Executes <paramref name="action"/> for each element and zero based index in the collection and returns the original collection.</summary>
      public IEnumerable<TItem> _forEach([InstantHandle] Action<TItem, int> action)
      {
         var index = 0;
         foreach(var item in @this)
         {
            action(item, index++);
         }

         return @this;
      }
   }
}
