using Compze.Contracts;
using Compze.DependencyInjection.Runtime.Resolution.Internal;

namespace Compze.DependencyInjection.Wiring.Registration;

/// <summary>
/// The Resolve/ResolveSet exclusivity check shared by every DI container adapter's <see cref="ServiceResolver"/> and
/// <see cref="ScopeResolver"/> wiring — see <see cref="ComponentRegistration.IsComponentSetMember"/> for what it enforces.
/// </summary>
/// <remarks>
/// Public only because adapter assemblies (<c>Compze.DependencyInjection.Microsoft</c> and siblings) need it and this project
/// grants them no internals visibility — it is adapter plumbing, not something application code should call directly.
/// </remarks>
static class ComponentSetExclusivityGuard
{
   public static object Resolve(Type serviceType, IReadOnlySet<Type> componentSetServiceTypes, Func<Type, object> resolve)
   {
      Contract.State.Assert(!componentSetServiceTypes.Contains(serviceType),
         () => $"Service type '{serviceType.FullName}' is registered as a component set member — resolve it via ResolveSet instead of Resolve.");
      return resolve(serviceType);
   }

   public static IEnumerable<object> ResolveSet(Type serviceType, IReadOnlySet<Type> singularServiceTypes, Func<Type, IEnumerable<object>> resolveSet)
   {
      Contract.State.Assert(!singularServiceTypes.Contains(serviceType),
         () => $"Service type '{serviceType.FullName}' is registered as a singular service — resolve it via Resolve instead of ResolveSet.");
      return resolveSet(serviceType);
   }
}
