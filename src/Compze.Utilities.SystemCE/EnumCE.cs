using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Compze.Utilities.SystemCE;

public static class EnumCE
{
   public static bool IsValid<TEnum>(this TEnum value) where TEnum : struct, Enum
      => Values<TEnum>().Contains(value);

   public static bool IsValid(this Enum value) => Values(value).Contains(value);

   private static IReadOnlySet<TEnum> Values<TEnum>() where TEnum : struct, Enum
      => TypeCache<TEnum>.ValidValues;

   private static IReadOnlySet<Enum> Values(Type enumType) => Cache.GetOrAdd(enumType, type => Enum.GetValues(type).Cast<Enum>().ToHashSet());
   private static IReadOnlySet<Enum> Values(Enum value) => Values(value.GetType());

   static class TypeCache<T> where T : struct, Enum
   {
      public static readonly IReadOnlySet<T> ValidValues = Enum.GetValues<T>().ToHashSet();
   }

   static readonly ConcurrentDictionary<Type, IReadOnlySet<Enum>> Cache = new();
}
