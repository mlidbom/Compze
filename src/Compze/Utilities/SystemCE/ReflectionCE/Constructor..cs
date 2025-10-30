using System;
using System.Collections.Concurrent;
using System.Reflection;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Utilities.SystemCE.ReflectionCE;

public static partial class Constructor
{
   internal static class For<TInstance>
   {
      internal static class DefaultConstructor
      {
         internal static readonly Func<TInstance> Instance = Compile.DefaultInstanceFactory<TInstance>();
      }

      internal static class WithArguments<TArgument1>
      {
         internal static readonly Func<TArgument1, TInstance> Instance = Compile.ForType<TInstance>().WithArguments<TArgument1>();
      }
   }

   internal static class ForGenericType<TGenericType>
   {
      // ReSharper disable once StaticMemberInGenericType
      static readonly ConcurrentDictionary<Type, Func<object, object>> Cache = new();

      internal static Func<object, object> WithArgument(Type argumentType) =>
         Cache.GetOrAdd(argumentType, _ => Compile.ForGenericType<TGenericType>().WithArgument(argumentType));
   }

   internal static bool HasDefaultConstructor(Type type) => type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) != null;

   public static object CreateInstance(Type type) => Result.ReturnNotNull(Activator.CreateInstance(type, nonPublic: true)); //Activator.CreateInstance is highly optimized nowadays. Compiling a constructor wins only when we don't need to do even a lookup by type.
}
