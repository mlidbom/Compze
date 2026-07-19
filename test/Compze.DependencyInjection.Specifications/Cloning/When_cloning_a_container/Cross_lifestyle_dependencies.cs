using Compze.DependencyInjection.Runtime;
using Compze.DependencyInjection.Runtime.Resolution;
using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.DependencyInjection.Wiring.Registration;
using Compze.Must;


namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class Cross_lifestyle_dependencies
{
   [DependencyInjectionContainerMatrix]
   public void clone_resolves_services_with_dependencies()
   {
      var sourceBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      sourceBuilder.Registrar.Register(
         Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService()),
         Scoped.For<IScopedService>().CreatedBy((ISingletonService s) => new ScopedServiceDependingOnSingleton(s))
      );

      using var source = sourceBuilder.Build();
      using var clone = source.CreateCloneContainerBuilder().Build();
      using var scope = clone.BeginScope();
      scope.Resolve<IScopedService>().Must().NotBeNull();
   }
}
