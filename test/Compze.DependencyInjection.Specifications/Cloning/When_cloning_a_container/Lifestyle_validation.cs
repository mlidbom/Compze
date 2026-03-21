using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class Lifestyle_validation
{
   [DependencyInjectionContainerMatrix]
   public void clone_preserves_AllowSingletonDependent_flag()
   {
      var sourceBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      sourceBuilder.Registrar.Register(
         TrackedTransient.For<ITransientService>().AllowSingletonDependent().CreatedBy(() => new TransientService()),
         Singleton.For<ISingletonService>().CreatedBy((ITransientService t) => new SingletonServiceDependingOnTransient(t))
      );

      using var source = sourceBuilder.Build();
      using var clone = source.CreateCloneContainerBuilder().Build();
      clone.Resolve<ISingletonService>().Must().NotBeNull();
   }

   [DependencyInjectionContainerMatrix]
   public void clone_preserves_AllowScopedDependent_flag()
   {
      var sourceBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      sourceBuilder.Registrar.Register(
         TrackedTransient.For<ITransientService>().AllowScopedDependent().CreatedBy(() => new TransientService()),
         Scoped.For<IScopedService>().CreatedBy((ITransientService t) => new ScopedServiceDependingOnTransient(t))
      );

      using var source = sourceBuilder.Build();
      using var clone = source.CreateCloneContainerBuilder().Build();
      using var scope = clone.BeginScope();
      scope.Resolve<IScopedService>().Must().NotBeNull();
   }
}
