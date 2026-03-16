using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class Lifestyle_validation
{
   [DependencyInjectionContainerMatrix]
   public void clone_preserves_AllowSingletonDependent_flag()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainer();
      source.Register(
         TrackedTransient.For<ITransientService>().AllowSingletonDependent().CreatedBy(() => new TransientService()),
         Singleton.For<ISingletonService>().CreatedBy((ITransientService t) => new SingletonServiceDependingOnTransient(t))
      );

      using var clone = source.Clone();
      clone.ServiceLocator.Resolve<ISingletonService>().Must().NotBeNull();
   }

   [DependencyInjectionContainerMatrix]
   public void clone_preserves_AllowScopedDependent_flag()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainer();
      source.Register(
         TrackedTransient.For<ITransientService>().AllowScopedDependent().CreatedBy(() => new TransientService()),
         Scoped.For<IScopedService>().CreatedBy((ITransientService t) => new ScopedServiceDependingOnTransient(t))
      );

      using var clone = source.Clone();
      using var scope = clone.ServiceLocator.BeginScope();
      scope.Resolve<IScopedService>().Must().NotBeNull();
   }
}
