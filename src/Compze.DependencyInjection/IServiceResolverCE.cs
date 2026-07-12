using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection;

// ReSharper disable once InconsistentNaming
public static class IServiceResolverCE
{

   public static TComponent Resolve<TComponent>(this IServiceResolver @this) where TComponent : class => (TComponent)@this.Resolve(typeof(TComponent));

   public static TComponent Resolve<TComponent>(this IScopeResolver @this) where TComponent : class => (TComponent)@this.Resolve(typeof(TComponent));

   public static TComponent Resolve<TComponent>(this IRootResolver @this) where TComponent : class => (TComponent)@this.Resolve(typeof(TComponent));

   ///<summary>
   /// Resolves every component registered as a member of the <typeparamref name="TComponent"/> component set — see
   /// <c>ForSet(...)</c>.
   ///</summary>
   /// <remarks>
   /// Compze registers set members in the order it received them, but the order elements come back in is whatever the underlying DI container's own collection resolution produces.
   /// </remarks>
   public static IEnumerable<TComponent> ResolveSet<TComponent>(this IServiceResolver @this) where TComponent : class => @this.ResolveSet(typeof(TComponent)).Cast<TComponent>();

   ///<summary>
   /// Resolves every component registered as a member of the <typeparamref name="TComponent"/> component set — see
   /// <c>ForSet(...)</c>.
   ///</summary>
   /// <remarks>
   /// Compze registers set members in the order it received them, but the order elements come back in is whatever the underlying DI container's own collection resolution produces.
   /// </remarks>
   public static IEnumerable<TComponent> ResolveSet<TComponent>(this IScopeResolver @this) where TComponent : class => @this.ResolveSet(typeof(TComponent)).Cast<TComponent>();

   ///<summary>
   /// Resolves every component registered as a member of the <typeparamref name="TComponent"/> component set — see
   /// <c>ForSet(...)</c>.
   ///</summary>
   /// <remarks>
   /// Compze registers set members in the order it received them, but the order elements come back in is whatever the underlying DI container's own collection resolution produces.
   /// </remarks>
   public static IEnumerable<TComponent> ResolveSet<TComponent>(this IRootResolver @this) where TComponent : class => @this.ResolveSet(typeof(TComponent)).Cast<TComponent>();
}
