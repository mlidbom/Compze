using Compze.Underscore;
using Compze.Internals.SystemCE.LinqCE;

namespace Compze.DependencyInjection.Abstractions;

//todo: rather than passing an untyped IRunMode around and using it to make decisions.
// Subtypes of IComponentRegistrar should be making the decisions, and tests should supply a different IComponentRegistrar than production code.
// The test version of the registrar would know about TestEnv, none of the production code should need any testing references or DbPool references etc.
public interface IComponentRegistrar
{
   IComponentRegistrar Register(params ComponentRegistration[] registrations);

   IComponentRegistrar Register(params Action<IComponentRegistrar>[] registrationMethods)
      => registrationMethods.ForEach(it => it(this))
                            .__(this);

   bool IsClone { get; }
   bool IsRegistered<TComponent>() where TComponent : class;

   TTestingRegistrar? TryGetTestingRegistrar<TTestingRegistrar>() where TTestingRegistrar : class;

   IComponentRegistrar Clone();
}

/// <summary>
/// Composes <see cref="IComponentRegistrar"/> (for registration) and <see cref="Build"/> (for finalization).
/// Registration is performed through <see cref="Registrar"/>. The builder itself has no registration methods.
/// After <see cref="Build"/> is called, the builder should not be used further — it is a spent object.
/// Not disposable: the built <see cref="IDependencyInjectionContainer"/> owns all resources.
/// </summary>
public interface IContainerBuilder
{
   IComponentRegistrar Registrar { get; }
   IDependencyInjectionContainer Build(ContainerOptions? options = null);
}

/// <summary>
/// A built container. Composes <see cref="IRootResolver"/>, <see cref="IScopeFactory"/>, and child container creation.
/// Does not inherit resolution or scope creation — exposes them via properties.
/// </summary>
public interface IDependencyInjectionContainer : IDisposable, IAsyncDisposable
{
   IRootResolver RootResolver { get; }
   IScopeFactory ScopeFactory { get; }
   IContainerBuilder CreateCloneContainerBuilder();

   /// <summary>
   /// Creates a child container builder. Unlike <see cref="CreateCloneContainerBuilder"/>, all singletons delegate to the parent by default
   /// (same instance, not disposed by child). Scoped and transient registrations are copied (fresh instances in child scopes).
   /// Additional registrations can be added to the returned builder before calling <see cref="IContainerBuilder.Build"/>.
   /// </summary>
   IContainerBuilder CreateChildContainerBuilder();
}

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
/// Enable it on the target's registration with <see cref="ServiceResolverExtensions.WithServiceResolver{TSpec}(TSpec)"/> <br/>
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

///<summary>
/// Every member of the <typeparamref name="TService"/> component set (<c>ForSet(...)</c>), as an ordinary constructor<br/>
/// dependency: a component that takes an <see cref="IComponentSet{TService}"/> through <c>CreatedBy(...)</c> receives the whole<br/>
/// set — the injectable counterpart of calling <see cref="IServiceResolver.ResolveSet"/> on a resolver.
///</summary>
///<remarks>
/// Never registered by user code: the container synthesizes the one <see cref="IComponentSet{TService}"/> registration per<br/>
/// component-set service type when it is built, from the set's <c>ForSet(...)</c> members. Its lifestyle follows the members' —<br/>
/// <see cref="Lifestyle.Singleton"/> when every member is a singleton, <see cref="Lifestyle.Scoped"/> otherwise — so a dependency<br/>
/// on the set is subject to exactly the same lifestyle validation as a direct dependency on the members would be.
///</remarks>
///<remarks>
/// The set holds no instances of its own: members resolve on enumeration, from the same container and scope that created the<br/>
/// set — the same thin-typed-view philosophy as <see cref="IServiceResolver{TService}"/>.
///</remarks>
public interface IComponentSet<out TService> : IEnumerable<TService> where TService : class;

///<summary>
/// An <see cref="IServiceResolver"/> capable only of resolving services registered as <see cref="Lifestyle.Singleton"/> and <see cref="Lifestyle.TrackedTransient"/>.
/// <see cref="Lifestyle.Scoped"/> services cannot be resolved from an instance.
/// </summary>
public interface IRootResolver : IServiceResolver;

///<summary>
/// <para>Instances are owned by an <see cref="IScope"/> and resolving components will only work within that scope.</para>
/// <para>All resolved components registered as <see cref="Lifestyle.Scoped"/> will resolve to the same instance and that instance will be disposed when the <see cref="IScope"/> is disposed</para>
/// </summary>
public interface IScopeResolver : IServiceResolver;

///<summary>
/// <para>>When resolved through <see cref="Resolver"/>> all services registered as <see cref="Lifestyle.Scoped"/> will resolve as the same exact instance, separate from the instance returned by any other <see cref="IScopeResolver"/></para>
/// <para>Dispose will dispose All <see cref="Lifestyle.Scoped"/> or <see cref="Lifestyle.TrackedTransient"/> services resolved through <see cref="Resolver"/></para>
///
/// </summary>
public interface IScope : IDisposable
{
   IScopeResolver Resolver { get; }
}

///<summary>Creates instances of <see cref="IScope"/>></summary>
public interface IScopeFactory
{
   IScope BeginScope();
}


///<summary>The supported service lifestyles</summary>
public enum Lifestyle
{
   ///<summary>Every call to <see cref="IServiceResolver.Resolve"/> will return the same instance for a service registered as <see cref="Singleton"/></summary>
   Singleton,

   ///<summary>
   /// <see cref="Scoped"/> services can only be resolved within an <see cref="IScope"/>, preferably through an <see cref="IScopeResolver"/>.
   /// <para>While inside a scope, every call to <see cref="IServiceResolver.Resolve"/> will return the same instance.</para>
   /// <para>Once the scope is disposed, all the <see cref="Scoped"/> instances are also disposed.</para>
   /// </summary>
   Scoped,

   ///<summary>
   /// Every call to <see cref="IServiceResolver.Resolve"/> will return a new unique instance of the service for a service registered as <see cref="TrackedTransient"/>.
   ///<para>If resolved within a scope, the instance will be disposed when the scope is disposed.</para>
   ///<para>If resolved outside a scope, the instance will be disposed when the container is disposed.</para>
   /// </summary>
   TrackedTransient
}
