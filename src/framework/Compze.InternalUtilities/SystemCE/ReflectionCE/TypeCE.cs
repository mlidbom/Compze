using System;
using System.Collections.Generic;
using System.Linq;
using static Compze.Contracts.Assert;

namespace Compze.SystemCE.ReflectionCE;

/// <summary>A collection of extensions to work with <see cref="Type"/></summary>
static class TypeCE
{
   public static string FullNameNotNull(this Type @this) => Result.ReturnNotNull(@this.FullName);

   /// ///<returns>true if <paramref name="me"/> implements the interface: <typeparamref name="TImplemented"/>. By definition true if <paramref name="me"/> == <typeparamref name="TImplemented"/>.</returns>
   public static bool Implements<TImplemented>(this Type me)
   {
      Argument.NotNull(me);

      if(!typeof(TImplemented).IsInterface)
      {
         throw new ArgumentException(nameof(TImplemented));
      }

      return typeof(TImplemented).IsAssignableFrom(me);
   }

   ///<returns>true if <paramref name="me"/> implements the interface: <paramref name="implemented"/>. By definition true if <paramref name="me"/> == <paramref name="implemented"/>.</returns>
   public static bool Implements(this Type me, Type implemented)
   {
      Argument.NotNull(me).NotNull(implemented);

      if(!implemented.IsInterface)
      {
         throw new ArgumentException(nameof(implemented));
      }

      if(me == implemented) { return true; }

      if(me is { IsInterface: true, IsGenericType: true } && me.GetGenericTypeDefinition() == implemented)
      {
         return true;
      }

      if(implemented.IsGenericTypeDefinition)
      {
         return
            me.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == implemented);
      }

      return me.GetInterfaces().Contains(implemented);
   }

   public static IEnumerable<Type> ClassInheritanceChain(this Type me)
   {
      var current = me;
      while(current != null)
      {
         yield return current;
         current = current.BaseType;
      }
   }

   public static bool Is<TOther>(this Type @this) => typeof(TOther).IsAssignableFrom(@this);

   public static string GetFullNameCompilable(this Type @this)
   {
      if(!@this.IsConstructedGenericType) return @this.FullName!.ReplaceInvariant("+", ".");

      var typeArguments = @this.GenericTypeArguments;
      var genericTypeName = @this.GetGenericTypeDefinition().GetFullNameCompilable().ReplaceInvariant($"`{typeArguments.Length}", "");

      var name = $"{genericTypeName}<{typeArguments.Select(type => type.GetFullNameCompilable()).Join(",")}>";

      return name;
   }
}
