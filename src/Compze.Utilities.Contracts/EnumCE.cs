using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Compze.Utilities.Contracts;

static class EnumCE
{
   public static bool IsValid<TEnum>(this TEnum value)
      where TEnum : struct, Enum => TypeCache<TEnum>.Values.Contains(value);

   public static bool IsValid(this Enum value)
      => Values(value).Contains(value);

   static readonly ConcurrentDictionary<Type, IReadOnlySet<Enum>> Cache = new();
   static IReadOnlySet<Enum> Values(Enum value) => Cache.GetOrAdd(value.GetType(), type => Enum.GetValues(type).Cast<Enum>().ToHashSet());

   static class TypeCache<T>
      where T : struct, Enum
   {
      public static readonly IReadOnlySet<T> Values = Enum.GetValues<T>().ToHashSet();
   }
}
