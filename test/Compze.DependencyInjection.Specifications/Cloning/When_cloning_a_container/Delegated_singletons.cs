using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class Delegated_singletons
{
   [DependencyInjectionContainerMatrix]
   public void Delegated_singleton_is_same_instance_in_source_and_clone()
   {
      var sourceBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      sourceBuilder.Registrar.Register(
         Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService())
                  .DelegateToParentServiceLocatorWhenCloning());

      using var source = sourceBuilder.Build();
      using var clone = source.CreateCloneContainerBuilder().Build();

      var sourceInstance = source.Resolve<ISingletonService>();
      var cloneInstance = clone.Resolve<ISingletonService>();

      cloneInstance.Must().Be(sourceInstance);
   }

   [DependencyInjectionContainerMatrix]
   public void Delegated_singleton_is_available_in_clone_scope()
   {
      var sourceBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      sourceBuilder.Registrar.Register(
         Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService())
                  .DelegateToParentServiceLocatorWhenCloning());

      using var source = sourceBuilder.Build();
      using var clone = source.CreateCloneContainerBuilder().Build();
      using var scope1 = clone.BeginScope();
      scope1.Resolve<ISingletonService>().Must().NotBeNull();
   }

   [DependencyInjectionContainerMatrix]
   public void Non_delegated_singleton_depending_on_delegated_singleton_resolves_correctly_in_clone()
   {
      var sourceBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      sourceBuilder.Registrar.Register(
         Singleton.For<IDelegatedDependency>().CreatedBy(() => new DelegatedDependency())
                  .DelegateToParentServiceLocatorWhenCloning(),
         Singleton.For<ISingletonService>()
                  .CreatedBy((IDelegatedDependency dep) => new SingletonWithDelegatedDep(dep)));

      using var source = sourceBuilder.Build();
      using var clone = source.CreateCloneContainerBuilder().Build();

      var sourceDepInstance = source.Resolve<IDelegatedDependency>();
      using var scope2 = clone.BeginScope();
      var cloneService = scope2.Resolve<ISingletonService>();
      cloneService.Must().NotBeNull();

      var cloneDepInstance = scope2.Resolve<IDelegatedDependency>();
      cloneDepInstance.Must().Be(sourceDepInstance);
   }

   [DependencyInjectionContainerMatrix]
   public void Singleton_factory_counter_in_clone_is_called_exactly_once()
   {
      var factoryCallCount = 0;

      var sourceBuilder = DependencyInjectionContainerFactory.CreateContainerBuilder();
      sourceBuilder.Registrar.Register(
         Singleton.For<ISingletonService>().CreatedBy(() =>
         {
            Interlocked.Increment(ref factoryCallCount);
            return new SingletonService();
         }));

      using var source = sourceBuilder.Build();
      using var clone = source.CreateCloneContainerBuilder().Build();

      clone.Resolve<ISingletonService>();
      clone.Resolve<ISingletonService>();

      using var scope3 = clone.BeginScope();
      scope3.Resolve<ISingletonService>();

      factoryCallCount.Must().Be(1);
   }
}

interface IDelegatedDependency;
class DelegatedDependency : IDelegatedDependency;

class SingletonWithDelegatedDep(IDelegatedDependency dep) : ISingletonService
{
   // ReSharper disable once UnusedMember.Global
   public IDelegatedDependency Dep { get; } = dep;
}
