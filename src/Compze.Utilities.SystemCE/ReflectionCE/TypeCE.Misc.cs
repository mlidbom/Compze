using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Compze.Contracts;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;

namespace Compze.Utilities.SystemCE.ReflectionCE;

/// <summary>A collection of extensions to work with <see cref="Type"/></summary>
public static partial class TypeCE
{
   public static TypeMethods Methods(this Type type) => new(type);
}

public class TypeMethods(Type type)
{
   readonly Type _type = type;

   MethodInfo GetToString() => _type.GetMethod("ToString", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null)._assert().NotNull();

   static readonly IReadOnlySet<Type> NoMeaningfulOverrideTypes = EnumerableCE.OfTypes<object, ValueType, Enum>().ToHashSet();
   public bool HasMeaningfulToStringOverride() => !NoMeaningfulOverrideTypes.Contains(GetToString().DeclaringType._assert().NotNull());
}
