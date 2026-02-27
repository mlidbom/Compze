using System;
using System.Collections.Generic;
using JetBrains.Annotations;

// ReSharper disable PossibleMultipleEnumeration

namespace Compze.Functional;

public static class ForEachExtensions
{
   /// <summary>Executes <paramref name="action"/> for each element in the collection and returns the original collection.</summary>
   /// <remarks>C# cannot infer <typeparamref name="TItem"/> from the constraint. Lambda parameters must be explicitly typed: <c>collection._forEach((MyItem it) => ...)</c></remarks>
   public static TCollection _forEach<TCollection, TItem, TReturn>(this TCollection @this, [InstantHandle] Func<TItem, TReturn> action) where TCollection : IEnumerable<TItem>
   {
      foreach(var item in @this)
      {
         action(item);
      }

      return @this;
   }

   /// <summary>Executes <paramref name="action"/> for each element in the collection and returns the original collection.</summary>
   /// <remarks>C# cannot infer <typeparamref name="TItem"/> from the constraint. Lambda parameters must be explicitly typed: <c>collection._forEach((MyItem it) => ...)</c></remarks>
   public static TCollection _forEach<TCollection, TItem>(this TCollection @this, [InstantHandle] Action<TItem> action) where TCollection : IEnumerable<TItem>
   {
      foreach(var item in @this)
      {
         action(item);
      }

      return @this;
   }

   /// <summary>Executes <paramref name="action"/> for each element and zero based index in the collection and returns the original collection.</summary>
   /// <remarks>C# cannot infer <typeparamref name="TItem"/> from the constraint. Lambda parameters must be explicitly typed: <c>collection._forEach((MyItem it, int index) => ...)</c></remarks>
   public static TCollection _forEach<TCollection, TItem>(this TCollection @this, [InstantHandle] Action<TItem, int> action) where TCollection : IEnumerable<TItem>
   {
      var index = 0;
      foreach(var item in @this)
      {
         action(item, index++);
      }

      return @this;
   }
}
