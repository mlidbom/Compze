using System;
using System.Collections.Concurrent;
using System.Reflection;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Utilities.SystemCE.ReflectionCE;

public static partial class Constructor
{
   public static class For<TInstance>
   {
      public static class DefaultConstructor
      {
         public static readonly Func<TInstance> Instance = Compile.DefaultInstanceFactory<TInstance>();
      }

      public static class WithArguments<TArgument1>
      {
         public static readonly Func<TArgument1, TInstance> Instance = Compile.ForType<TInstance>().WithArguments<TArgument1>();
      }
   }

   static readonly ConcurrentDictionary<Type, GenericTypeConstructor> GenericTypeConstructors = new();
   public static GenericTypeConstructor ForGenericType(Type genericType) => GenericTypeConstructors.GetOrAdd(genericType, it => new GenericTypeConstructor(it));

   public class GenericTypeConstructor(Type genericType)
   {
      readonly Type _genericType = genericType;

      static readonly ConcurrentDictionary<Type, Func<object, object>> ArgumentTypeConstructorCache = new();

      public Func<object, object> WithArgument(Type argumentType) =>
         ArgumentTypeConstructorCache.GetOrAdd(argumentType,
                        _ => Compile.ForGenericType(_genericType).WithArgument(argumentType)
         );
   }

   public static bool HasDefaultConstructor(Type type) => type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) != null;

   public static object CreateInstance(Type type) => ReturnValue.ReturnNotNull(Activator.CreateInstance(type, nonPublic: true)); //Activator.CreateInstance is highly optimized nowadays. Compiling a constructor wins only when we don't need to do even a lookup by type.
}
