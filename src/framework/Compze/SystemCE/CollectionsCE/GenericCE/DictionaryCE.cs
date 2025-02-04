using System;
using System.Collections.Generic;
using static Compze.Contracts.Assert;

namespace Compze.SystemCE.CollectionsCE.GenericCE;

///<summary>Helpers for working with dictionaries</summary>
static class DictionaryCE
{
   /// <summary>
   /// If <paramref name="key"/> exists in me <paramref name="me"/> it is returned.
   /// If not <paramref name="constructor"/> is used to create a new value that is inserted into <paramref name="me"/> and returned.
   /// </summary>
   public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> me, TKey key, Func<TValue> constructor) where TKey : notnull
   {
      Argument.NotNull(me).NotNull(key).NotNull(constructor);

      if(me.TryGetValue(key, out var value))
      {
         return value;
      }

      value = constructor();
      me.Add(key, value);
      return value;
   }

   /// <summary>
   /// If <paramref name="key"/> exists in me <paramref name="me"/> it is returned if not it is inserted from the default constructor and returned.
   /// </summary>
   public static TValue GetOrAddDefault<TKey, TValue>(this IDictionary<TKey, TValue> me, TKey key) where TValue : new()
                                                                                                   where TKey : notnull
   {
      Argument.NotNull(me).NotNull(key);
      //Originally written to delegate to the above method. Believe it or not this causes a performance decrease that is actually significant in tight loops.
      if(me.TryGetValue(key, out var value))
      {
         return value;
      }

      value = new TValue();
      me.Add(key, value);
      return value;
   }
}
