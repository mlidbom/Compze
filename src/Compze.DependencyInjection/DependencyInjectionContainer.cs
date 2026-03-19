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

   IContainerBuilder IDependencyInjectionContainer.Clone()
   {
      Log.Info($"Cloning IDependencyInjectionContainer: {GetHashCode()}");
      IRootResolver sourceRootResolver = (IRootResolver)this;
      var cloneBuilder = CreateBuilderForClone(_sourceRegistrar.Clone());
      cloneBuilder.IsClone = true;

      _registrations
        .ForEach(action: registration => cloneBuilder.Register(registration.CreateCloneRegistration(sourceRootResolver)));

      return cloneBuilder;
   }

   protected abstract ContainerBuilderBase CreateBuilderForClone(IComponentRegistrar clonedRegistrar);

   public abstract void Dispose();
   public abstract ValueTask DisposeAsync();
}
