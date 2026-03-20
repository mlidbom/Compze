using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.LinqCE;

namespace Compze.DependencyInjection;

public abstract class DependencyInjectionContainer : IDependencyInjectionContainer
{
   static readonly ILogger Log = CompzeLogger.For<DependencyInjectionContainer>();

   readonly IReadOnlyList<ComponentRegistration> _registrations;
   readonly IComponentRegistrar _sourceRegistrar;

   protected DependencyInjectionContainer(IReadOnlyList<ComponentRegistration> registrations, IComponentRegistrar sourceRegistrar)
   {
      _registrations = registrations;
      _sourceRegistrar = sourceRegistrar;
   }

   IRootResolver IDependencyInjectionContainer.RootResolver => (IRootResolver)this;
   IScopeFactory IDependencyInjectionContainer.ScopeFactory => (IScopeFactory)this;

   IContainerBuilder IDependencyInjectionContainer.CreateCloneContainerBuilder() => CreateCloneContainerBuilder();

   public virtual ContainerBuilder CreateCloneContainerBuilder()
   {
      Log.Info($"Cloning IDependencyInjectionContainer: {GetHashCode()}");
      IRootResolver sourceRootResolver = (IRootResolver)this;
      var cloneBuilder = CreateConcreteBuilder(_sourceRegistrar.Clone());
      cloneBuilder.IsClone = true;

      _registrations
        .ForEach(action: registration => cloneBuilder.Register(registration.CreateCloneRegistration(sourceRootResolver)));

      return cloneBuilder;
   }

   IContainerBuilder IDependencyInjectionContainer.CreateChildContainerBuilder() => CreateChildContainerBuilder();

   public virtual ContainerBuilder CreateChildContainerBuilder()
   {
      Log.Info($"Creating child container builder from IDependencyInjectionContainer: {GetHashCode()}");
      IRootResolver parentRootResolver = (IRootResolver)this;
      var childBuilder = CreateConcreteBuilder(_sourceRegistrar.Clone());

      _registrations
        .ForEach(action: registration => childBuilder.Register(registration.CreateChildRegistration(parentRootResolver)));

      return childBuilder;
   }

   protected abstract ContainerBuilder CreateConcreteBuilder(IComponentRegistrar registrar);

   public abstract void Dispose();
   public abstract ValueTask DisposeAsync();
}
