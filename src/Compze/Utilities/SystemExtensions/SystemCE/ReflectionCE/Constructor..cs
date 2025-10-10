using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Utilities.SystemCE.ReflectionCE;

public static partial class Constructor
{
   internal static class For<TInstance>
   {
      internal static class DefaultConstructor
      {
         internal static readonly Func<TInstance> Instance = CreateInstanceFactory();

         static Func<TInstance> CreateInstanceFactory() =>
            typeof(IStaticInstancePropertySingleton<TInstance>).IsAssignableFrom(typeof(TInstance))
               ? CompileStaticInstancePropertyDelegate()
               : Compile.ForReturnType<TInstance>().DefaultConstructor();

         static PropertyInfo? ImplicitImplementationProperty() => typeof(TInstance).GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                                                                   .SingleOrDefault(prop => prop.Name == nameof(IStaticInstancePropertySingleton<TInstance>.Instance) && prop.PropertyType == typeof(TInstance));

         static PropertyInfo ExplicitImplementationProperty()
         {
            // When a class uses explicit interface implementation, the property name includes the full interface name
            return typeof(TInstance)
                  .GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                  .SingleOrDefault(prop =>
                                      prop.Name.Contains(nameof(IStaticInstancePropertySingleton<TInstance>), StringComparison.Ordinal) &&
                                      prop.Name.Contains(nameof(IStaticInstancePropertySingleton<TInstance>.Instance), StringComparison.Ordinal) &&
                                      prop.PropertyType == typeof(TInstance))
                  .NotNull(() => $"This should be impossible, but it seems {typeof(TInstance).FullName} does not implement {typeof(IStaticInstancePropertySingleton<TInstance>).FullName}");
         }

         static Func<TInstance> CompileStaticInstancePropertyDelegate()
         {
            var instanceProperty = ImplicitImplementationProperty() ?? ExplicitImplementationProperty();

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
