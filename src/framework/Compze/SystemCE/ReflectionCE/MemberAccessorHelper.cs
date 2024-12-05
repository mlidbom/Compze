﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Compze.Contracts.Deprecated;

namespace Compze.SystemCE.ReflectionCE;

///<summary>Provides high performance access to object fields and properties.</summary>
static class MemberAccessorHelper
{
   static readonly IDictionary<Type, Func<object, object>[]> TypeFields = new ConcurrentDictionary<Type, Func<object, object>[]>();

   static Func<object, object> BuildFieldGetter(FieldInfo field)
   {
      Contracts.Assert.Argument.NotNull(field);

      Contract.Assert.That(field.DeclaringType != null, "field.DeclaringType != null");

      var obj = Expression.Parameter(typeof(object), "obj");

      return Expression.Lambda<Func<object, object>>(
         Expression.Convert(
            Expression.Field(
               // ReSharper disable once AssignNullToNotNullAttribute
               Expression.Convert(obj, field.DeclaringType),
               field),
            typeof(object)),
         obj).Compile();
   }

   ///<summary>Returns functions that when invoked will return the values of the fields an properties in an instance of the supplied type.</summary>
   public static Func<object, object>[] GetFieldGetters(Type type)
   {
      Contracts.Assert.Argument.NotNull(type);

      return InnerGetFields(type);
   }

   static Func<object, object>[] InnerGetFields(Type type)
   {

      if (!TypeFields.TryGetValue(type, out var fields))
      {
         var newFields = new List<Func<object, object>>();
         if (!type.IsPrimitive)
         {
            newFields.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Select(BuildFieldGetter));

            var baseType = type.BaseType;
            if (baseType != null && baseType != typeof(object))
            {
               newFields.AddRange(GetFieldGetters(baseType));
            }
         }
         TypeFields[type] = fields = newFields.ToArray();
      }
      return fields;
   }
}

///<summary>Provides high performance access to object fields and properties.</summary>
static class MemberAccessorHelper<T>
{
   // ReSharper disable StaticFieldInGenericType
   static readonly Func<object, object>[] Fields = MemberAccessorHelper.GetFieldGetters(typeof(T));
   // ReSharper restore StaticFieldInGenericType

   ///<summary>Returns functions that when invoked will return the values of the fields an properties in an instance of the supplied type.</summary>
   public static Func<object, object?>[] GetFieldGetters(Type type)
   {
      Contracts.Assert.Argument.NotNull(type);

      return type == typeof(T) ? Fields : MemberAccessorHelper.GetFieldGetters(type);
   }
}