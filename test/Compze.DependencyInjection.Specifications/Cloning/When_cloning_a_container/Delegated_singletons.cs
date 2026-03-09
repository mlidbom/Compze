using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Cloning.When_cloning_a_container;

public class Delegated_singletons
{
   [DependencyInjectionContainerMatrix]
   public void Delegated_singleton_is_same_instance_in_source_and_clone()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainer();
      source.Register(
         Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService())
                  .DelegateToParentServiceLocatorWhenCloning());

      _ = source.ServiceLocator;

      using var clone = source.Clone();

      var sourceInstance = source.ServiceLocator.Resolve<ISingletonService>();
      var cloneInstance = clone.ServiceLocator.Resolve<ISingletonService>();

      cloneInstance.Must().Be(sourceInstance);
   }

   [DependencyInjectionContainerMatrix]
   public void Delegated_singleton_is_available_in_clone_scope()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainer();
      source.Register(
         Singleton.For<ISingletonService>().CreatedBy(() => new SingletonService())
                  .DelegateToParentServiceLocatorWhenCloning());

      using var clone = source.Clone();
      using(clone.ServiceLocator.BeginScope())
      {
         clone.ServiceLocator.Resolve<ISingletonService>().Must().NotBeNull();
      }
   }

   [DependencyInjectionContainerMatrix]
   public void Non_delegated_singleton_depending_on_delegated_singleton_resolves_correctly_in_clone()
   {
      using var source = DependencyInjectionContainerFactory.CreateContainer();
      source.Register(
         Singleton.For<IDelegatedDependency>().CreatedBy(() => new DelegatedDependency())
                  .DelegateToParentServiceLocatorWhenCloning(),
         Singleton.For<ISingletonService>()
                  .CreatedBy((IDelegatedDependency dep) => new SingletonWithDelegatedDep(dep)));

      _ = source.ServiceLocator;

      using var clone = source.Clone();

      var sourceDepInstance = source.ServiceLocator.Resolve<IDelegatedDependency>();
      using(clone.ServiceLocator.BeginScope())
      {
         var cloneService = clone.ServiceLocator.Resolve<ISingletonService>();
         cloneService.Must().NotBeNull();

         var cloneDepInstance = clone.ServiceLocator.Resolve<IDelegatedDependency>();
         cloneDepInstance.Must().Be(sourceDepInstance);
      }
   }

   [DependencyInjectionContainerMatrix]
   public void Singleton_factory_counter_in_clone_is_called_exactly_once()
   {
      var factoryCallCount = 0;

      using var source = DependencyInjectionContainerFactory.CreateContainer();
      source.Register(
         Singleton.For<ISingletonService>().CreatedBy(() =>
         {
            Interlocked.Increment(ref factoryCallCount);
            return new SingletonService();
         }));

      using var clone = source.Clone();

      clone.ServiceLocator.Resolve<ISingletonService>();
      clone.ServiceLocator.Resolve<ISingletonService>();

      using(clone.ServiceLocator.BeginScope())
      {
         clone.ServiceLocator.Resolve<ISingletonService>();
      }

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
