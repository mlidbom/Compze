using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static Compze.Contracts.Assert;

namespace Compze.SystemCE.ReflectionCE;

public static partial class Constructor
{
   internal static class For<TInstance>
   {
      internal static class DefaultConstructor
      {
         internal static readonly Func<TInstance> Instance = CreateInstanceFactory();
         static Func<TInstance> CreateInstanceFactory() =>
            typeof(TInstance).Is<IStaticInstancePropertySingleton>()
               ? CompileStaticInstancePropertyDelegate()
               : Compile.ForReturnType<TInstance>().DefaultConstructor();

         static Func<TInstance> CompileStaticInstancePropertyDelegate()
         {
            var instanceProperty = typeof(TInstance).GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                                    .SingleOrDefault(prop => prop.Name == "Instance" && prop.PropertyType == typeof(TInstance))
                                ?? throw new Exception($"{nameof(IStaticInstancePropertySingleton)} implementation: {typeof(TInstance).GetFullNameCompilable()} does not have a public property named Instance of of the same type.");

            return Expression.Lambda<Func<TInstance>>(Expression.Property(null, instanceProperty)).Compile();
         }
      }

      internal static class WithArguments<TArgument1>
      {
         internal static readonly Func<TArgument1, TInstance> Instance = Compile.ForReturnType<TInstance>().WithArguments<TArgument1>();
      }
   }

   internal static bool HasDefaultConstructor(Type type) => type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) != null;

   public static object CreateInstance(Type type) => Result.ReturnNotNull(Activator.CreateInstance(type, nonPublic: true)); //Activator.CreateInstance is highly optimized nowadays. Compiling a constructor wins only when we don't need to do even a lookup by type.
}