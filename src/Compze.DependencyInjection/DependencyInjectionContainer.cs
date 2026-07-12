using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.LinqCE;

namespace Compze.DependencyInjection;

public abstract class DependencyInjectionContainer : IDependencyInjectionContainer
{
   static readonly ILogger Log = CompzeLogger.For<DependencyInjectionContainer>();

   readonly IReadOnlyList<ComponentRegistration> _registrations;
   readonly IComponentRegistrar _sourceRegistrar;
   readonly IReadOnlySet<Type> _singularServiceTypes;
   readonly IReadOnlySet<Type> _componentSetServiceTypes;

   protected DependencyInjectionContainer(IReadOnlyList<ComponentRegistration> registrations, IComponentRegistrar sourceRegistrar)
   {
      _registrations = registrations;
      _sourceRegistrar = sourceRegistrar;
      _singularServiceTypes = registrations.Where(it => !it.IsComponentSetMember).SelectMany(it => it.ServiceTypes).ToHashSet();
      _componentSetServiceTypes = registrations.Where(it => it.IsComponentSetMember).SelectMany(it => it.ServiceTypes).ToHashSet();
   }

   /// <summary>
   /// Resolves the singular instance of <paramref name="serviceType"/>. Delegates to <see cref="ResolveCore"/> after asserting
   /// <paramref name="serviceType"/> was not registered as a component set member (<c>ForSet(...)</c>) — those are only
   /// resolvable through <see cref="ResolveSet"/>.
   /// </summary>
   public object Resolve(Type serviceType) => ComponentSetExclusivityGuard.Resolve(serviceType, _componentSetServiceTypes, ResolveCore);

   /// <summary>
   /// Resolves every component registered as a member of the <paramref name="serviceType"/> component set (<c>ForSet(...)</c>).
   /// Delegates to <see cref="ResolveSetCore"/> after asserting <paramref name="serviceType"/> was not registered as a singular
   /// service — those are only resolvable through <see cref="Resolve"/>.
   /// </summary>
   public IEnumerable<object> ResolveSet(Type serviceType) => ComponentSetExclusivityGuard.ResolveSet(serviceType, _singularServiceTypes, ResolveSetCore);

   /// <summary>The concrete container's own singular resolution — see <see cref="Resolve"/> for the exclusivity guard around it.</summary>
   protected abstract object ResolveCore(Type serviceType);

   /// <summary>The concrete container's own component-set resolution — see <see cref="ResolveSet"/> for the exclusivity guard around it.</summary>
   protected abstract IEnumerable<object> ResolveSetCore(Type serviceType);

#pragma warning disable CA1033 // Concrete subclasses always implement IRootResolver and IScopeFactory themselves; these explicit-interface impls just downcast and there is nothing for subclasses to override.
   IRootResolver IDependencyInjectionContainer.RootResolver => (IRootResolver)this;
   IScopeFactory IDependencyInjectionContainer.ScopeFactory => (IScopeFactory)this;
#pragma warning restore CA1033

   IContainerBuilder IDependencyInjectionContainer.CreateCloneContainerBuilder() => CreateCloneContainerBuilder();

   public virtual ContainerBuilder CreateCloneContainerBuilder()
   {
      Log.Info($"Cloning IDependencyInjectionContainer: {GetHashCode()}");
      IRootResolver sourceRootResolver = (IRootResolver)this;
      var cloneBuilder = CreateConcreteBuilder(_sourceRegistrar.Clone());
      cloneBuilder.IsClone = true;

      _registrations
        .ForEach(action: registration => cloneBuilder.Registrar.Register(registration.CreateCloneRegistration(sourceRootResolver)));

      return cloneBuilder;
   }

   IContainerBuilder IDependencyInjectionContainer.CreateChildContainerBuilder() => CreateChildContainerBuilder();

   public virtual ContainerBuilder CreateChildContainerBuilder()
   {
      Log.Info($"Creating child container builder from IDependencyInjectionContainer: {GetHashCode()}");
      IRootResolver parentRootResolver = (IRootResolver)this;
      var childBuilder = CreateConcreteBuilder(_sourceRegistrar.Clone());

      _registrations
        .ForEach(action: registration => childBuilder.Registrar.Register(registration.CreateChildRegistration(parentRootResolver)));

      return childBuilder;
   }

   protected abstract ContainerBuilder CreateConcreteBuilder(IComponentRegistrar registrar);

   public abstract void Dispose();
   public abstract ValueTask DisposeAsync();
}
