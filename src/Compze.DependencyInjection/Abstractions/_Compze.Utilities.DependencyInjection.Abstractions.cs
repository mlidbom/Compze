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
   IDependencyInjectionContainer Build();
}

/// <summary>
/// A built container. Composes <see cref="IRootResolver"/>, <see cref="IScopeFactory"/>, and child container creation.
/// Does not inherit resolution or scope creation — exposes them via properties.
/// </summary>
public interface IDependencyInjectionContainer : IDisposable, IAsyncDisposable
{
   IRootResolver RootResolver { get; }
   IScopeFactory ScopeFactory { get; }
   IContainerBuilder Clone();
}

///<summary>Resolves instances of services registered in the container.</summary>
public interface IServiceResolver
{
   object Resolve(Type serviceType);
}

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
