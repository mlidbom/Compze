using System.Collections.Concurrent;
using Compze.Contracts;

namespace Compze.Internals.SystemCE.ReflectionCE;

/// <summary>A collection of extensions to work with <see cref="Type"/></summary>
public static partial class TypeCE
{
   public static string FullNameNotNull(this Type @this) => @this.FullName._assert().NotNull();

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
   public static bool Implements(this Type me, Type implemented) => Argument.NotNull2(me, implemented).Assert(implemented.IsInterface)._(() =>
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

   static IEnumerable<Type> ListGenericInterfaces(this Type @this, Type genericInterface)
   {
      return @this.ListGenericInterfaces()
                  .Where(it => it.GetGenericTypeDefinition() == genericInterface);
   }

   static readonly ConcurrentDictionary<Type, List<Type>> GenericInterfacesByType = new();

   static IEnumerable<Type> ListGenericInterfaces(this Type @this) =>
      GenericInterfacesByType.GetOrAdd(@this, it => it.GetInterfaces().Where(it2 => it2.IsGenericType).ToList());

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

   public static bool IsOpenGenericType(this Type type) => type.ContainsGenericParameters;

   public static bool Is<TOther>(this Type @this) => typeof(TOther).IsAssignableFrom(@this);

   public static string GetFullNameCompilable(this Type @this)
   {
      if(!@this.IsConstructedGenericType) return @this.FullName!.ReplaceOrdinal("+", ".");

      var typeArguments = @this.GenericTypeArguments;
      var genericTypeName = @this.GetGenericTypeDefinition().GetFullNameCompilable().ReplaceOrdinal($"`{typeArguments.Length}", "");

      var name = $"{genericTypeName}<{typeArguments.Select(type => type.GetFullNameCompilable()).Join(",")}>";

      return name;
   }

   public static bool IsAssignableToOrFrom(this Type @this, Type other) => @this.IsAssignableFrom(other) || @this.IsAssignableTo(other);
}
