using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Compze.Functional;
using Compze.Contracts;
using static Compze.Contracts.Assert;

namespace Compze.Utilities.SystemCE.ReflectionCE;

/// <summary>A collection of extensions to work with <see cref="Type"/></summary>
public static partial class TypeCE
{
   public static string FullNameNotNull(this Type @this) => ReturnValue.ReturnNotNull(@this.FullName);

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
   public static bool Implements(this Type me, Type implemented) => Argument.NotNull(me).NotNull(implemented).Is(implemented.IsInterface)._then(() =>
   {
      if(me == implemented) { return true; }

      if(me is { IsInterface: true, IsGenericType: true } && me.GetGenericTypeDefinition() == implemented)
      {
         return true;
      }

      if(implemented.IsGenericTypeDefinition)
      {
         return me.ImplementsGenericInterface(implemented);
      }

      return me.GetInterfaces().Contains(implemented);
   });

   public static Type GetGenericInterface(this Type @this, Type implementedGenericInterface) =>
      @this.ListGenericInterfaces(implementedGenericInterface).Single();

   public static IEnumerable<Type> ListGenericInterfaces(this Type @this, Type genericInterface)
   {
      return @this.ListGenericInterfaces()
                  .Where(it => it.GetGenericTypeDefinition() == genericInterface);
   }

   static readonly ConcurrentDictionary<Type, List<Type>> GenericInterfacesByType = new();
   public static IEnumerable<Type> ListGenericInterfaces(this Type @this) =>
      GenericInterfacesByType.GetOrAdd(@this, it => it.GetInterfaces().Where(it => it.IsGenericType).ToList());

   public static bool ImplementsGenericInterface(this Type @this, Type implementedGenericInterface) =>
      @this.ListGenericInterfaces(implementedGenericInterface).Any();

   public static IEnumerable<Type> ClassInheritanceChain(this Type me)
   {
      var current = me;
      while(current != null)
      {
         yield return current;
         current = current.BaseType;
      }
   }

   public static bool InHerits(this Type @this, Type baseClass) =>
      baseClass.IsGenericTypeDefinition
         ? @this.InheritsFromGenericClassDefinition(baseClass)
         : @this.ClassInheritanceChain().Any(it => it == baseClass);

   public static Type GetGenericBaseClass(this Type @this, Type genericBaseClassTypeDefinition) =>
      @this.TryGetGenericBaseClass(genericBaseClassTypeDefinition) ?? throw new Exception($"{@this.FullName} does not inherit from {genericBaseClassTypeDefinition.FullName}");

   public static Type? TryGetGenericBaseClass(this Type @this, Type genericBaseClassTypeDefinition) =>
      @this.GenericBaseClasses()
           .SingleOrDefault(it => it.GetGenericTypeDefinition() == genericBaseClassTypeDefinition);

   public static IEnumerable<Type> GenericBaseClasses(this Type @this) =>
      @this.ClassInheritanceChain()
           .Where(it => it.IsGenericType);

   public static IEnumerable<Type> GenericBaseClassGenericTypeDefinitions(this Type @this) =>
      @this.GenericBaseClasses()
           .Select(it => it.GetGenericTypeDefinition());

   public static bool InheritsFromGenericClassDefinition(this Type @this, Type genericBaseClass) =>
      @this.GenericBaseClassGenericTypeDefinitions()
           .Any(it => it == genericBaseClass);

   public static bool IsOpenGenericType(this Type type) => type.ContainsGenericParameters;

   public static bool Is<TOther>(this object @this) => @this is TOther;

   public static bool Is<TOther>(this Type @this) => typeof(TOther).IsAssignableFrom(@this);

   public static string GetFullNameCompilable(this Type @this)
   {
      if(!@this.IsConstructedGenericType) return @this.FullName!.ReplaceCE("+", ".");

      var typeArguments = @this.GenericTypeArguments;
      var genericTypeName = @this.GetGenericTypeDefinition().GetFullNameCompilable().ReplaceCE($"`{typeArguments.Length}", "");

      var name = $"{genericTypeName}<{typeArguments.Select(type => type.GetFullNameCompilable()).Join(",")}>";

      return name;
   }

   public static bool IsAssignableToOrFrom(this Type @this, Type other) => @this.IsAssignableFrom(other) || @this.IsAssignableTo(other);
}
