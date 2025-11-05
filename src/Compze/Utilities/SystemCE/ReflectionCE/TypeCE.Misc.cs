using System;
using System.Linq;
using System.Reflection;

namespace Compze.Utilities.SystemCE.ReflectionCE;

/// <summary>A collection of extensions to work with <see cref="Type"/></summary>
static partial class TypeCE
{
   public static TypeMethods Methods(this Type type) => new(type);
}

class TypeMethods(Type type)
{
   readonly Type _type = type;

   public MethodInfo? TryGetInstance(string name, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance) => _type.GetMethod(name, flags);
   public MethodInfo GetInstance(string name, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance) => TryGetInstance(name, flags).NotNull();

   public MethodInfo GetToString() => _type.GetMethod("ToString", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null).NotNull();

   public bool HasMeaningfulToStringOverride()
   {
      var toStringMethod = _type.Methods().GetToString();

      if(toStringMethod.DeclaringType == null)
         return true;

      var noMeaningfulOverrideTypes = new[]
                                      {
                                         typeof(object),
                                         typeof(ValueType),
                                         typeof(Enum)
                                      };

      return !noMeaningfulOverrideTypes.Contains(toStringMethod.DeclaringType);
   }
}
