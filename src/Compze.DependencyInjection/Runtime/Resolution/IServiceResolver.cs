using Compze.DependencyInjection.Wiring.Registration;

namespace Compze.DependencyInjection.Runtime.Resolution;

///<summary>
/// Resolves instances of services registered in the container.
///<para>
/// Unlike <see cref="IScopeResolver"/> and <see cref="IRootResolver"/> this type provides you with no hint as to the context you are running in, whether there is a <see cref="IScope"/> or not<br/>
/// Prefer using <see cref="IScopeResolver"/> or <see cref="IRootResolver"/> whenever possible so you are not hiding this vital information from the next person to read the code.
/// When using this interface the reader will have to either know the context by heart, or go researching the call stack to trying and figure it out.
/// </para>
/// </summary>
public interface IServiceResolver
{
   object Resolve(Type serviceType);

   ///<summary>
   /// Resolves every component registered as a member of the <paramref name="serviceType"/> component set — see
   /// <c>ForSet(...)</c> in <c>Compze.DependencyInjection.Singleton</c>/<c>Scoped</c>/<c>TrackedTransient</c>.
   /// Returns an empty sequence if no component set member is registered for <paramref name="serviceType"/>.
   ///</summary>
   /// <remarks>
   /// Registrations are handed to the underlying container in the order they were received — Compze does not reorder them —
   /// but the order they come back out in is whatever the underlying container's own collection resolution produces.
   /// </remarks>
   IEnumerable<object> ResolveSet(Type serviceType);
}


///<summary>
/// A typed resolver for a single service. Each call to <see cref="Resolve"/> resolves the current
/// <typeparamref name="TService"/> from the same container and scope that created this resolver.
///</summary>
///<remarks>
/// Commonly used to break a constructor-injection cycle.
/// Have one side take an <see cref="IServiceResolver{TService}"/> of the other<br/>
/// instead of the service itself.
/// Enable it on the target's registration with <see cref="ComponentRegistrationWithoutInstantiationSpecServiceResolverExtensions"/> <br/>
/// then depend on <see cref="IServiceResolver{TService}"/> exactly as you would depend on <typeparamref name="TService"/> itself.
///</remarks>
///<remarks>
/// Call <see cref="Resolve"/> AFTER construction — if you call it in the constructor you should just take a dependency directly on the resolved type instead.<br/>
///</remarks>
public interface IServiceResolver<out TService> where TService : class
{
   ///<summary>
   /// Resolves the current <typeparamref name="TService"/>.<br/>
   /// Call after construction, never during it — see <see cref="IServiceResolver{TService}"/>.
   ///</summary>
   TService Resolve();
}
