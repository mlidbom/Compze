using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection;

// ReSharper disable once InconsistentNaming
public static class IServiceResolverCE
{

   public static TComponent Resolve<TComponent>(this IServiceResolver @this) where TComponent : class => (TComponent)@this.Resolve(typeof(TComponent));

   public static TComponent Resolve<TComponent>(this IScopeResolver @this) where TComponent : class => (TComponent)@this.Resolve(typeof(TComponent));

   public static TComponent Resolve<TComponent>(this IRootResolver @this) where TComponent : class => (TComponent)@this.Resolve(typeof(TComponent));
}
