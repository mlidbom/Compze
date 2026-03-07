using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Transient_lifestyle;

public class When_a_transient_depends_on_other_services
{
   [DependencyInjectionContainerMatrix]
   public void transient_can_depend_on_a_singleton()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();
      container.Register(
         Singleton.For<ISingletonDependency>().CreatedBy(() => new SingletonDependency()),
         TrackedTransient.For<IServiceWithDependency>().CreatedBy((ISingletonDependency dep) => new ServiceWithDependency(dep))
      );

      var serviceLocator = container.ServiceLocator;
      var first = serviceLocator.Resolve<IServiceWithDependency>();
      var second = serviceLocator.Resolve<IServiceWithDependency>();

      first.Must().NotBe(second);
      ((ServiceWithDependency)first).Dependency.Must().Be(((ServiceWithDependency)second).Dependency);
   }

   [DependencyInjectionContainerMatrix]
   public void transient_can_depend_on_another_transient()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();
      container.Register(
         TrackedTransient.For<ITransientDependency>().CreatedBy(() => new TransientDependency()),
         TrackedTransient.For<IServiceWithTransientDependency>().CreatedBy((ITransientDependency dep) => new ServiceWithTransientDependency(dep))
      );

      var serviceLocator = container.ServiceLocator;
      var first = (ServiceWithTransientDependency)serviceLocator.Resolve<IServiceWithTransientDependency>();
      var second = (ServiceWithTransientDependency)serviceLocator.Resolve<IServiceWithTransientDependency>();

      first.Must().NotBe(second);
      first.Dependency.Must().NotBe(second.Dependency);
   }

   [DependencyInjectionContainerMatrix]
   public void untracked_transient_can_depend_on_a_singleton()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();
      container.Register(
         Singleton.For<ISingletonDependency>().CreatedBy(() => new SingletonDependency()),
         Transient.For<IServiceWithDependency>().CreatedBy((ISingletonDependency dep) => new ServiceWithDependency(dep))
      );

      var serviceLocator = container.ServiceLocator;
      var first = serviceLocator.Resolve<IServiceWithDependency>();
      var second = serviceLocator.Resolve<IServiceWithDependency>();

      first.Must().NotBe(second);
      ((ServiceWithDependency)first).Dependency.Must().Be(((ServiceWithDependency)second).Dependency);
   }

   [DependencyInjectionContainerMatrix]
   public void untracked_transient_can_depend_on_another_untracked_transient()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();
      container.Register(
         Transient.For<ITransientDependency>().CreatedBy(() => new TransientDependency()),
         Transient.For<IServiceWithTransientDependency>().CreatedBy((ITransientDependency dep) => new ServiceWithTransientDependency(dep))
      );

      var serviceLocator = container.ServiceLocator;
      var first = (ServiceWithTransientDependency)serviceLocator.Resolve<IServiceWithTransientDependency>();
      var second = (ServiceWithTransientDependency)serviceLocator.Resolve<IServiceWithTransientDependency>();

      first.Must().NotBe(second);
      first.Dependency.Must().NotBe(second.Dependency);
   }

   interface ISingletonDependency;
   class SingletonDependency : ISingletonDependency;

   interface IServiceWithDependency;
#pragma warning disable CS9113 // Parameter is unread.
   class ServiceWithDependency(ISingletonDependency dependency) : IServiceWithDependency
   {
      public ISingletonDependency Dependency => dependency;
   }

   interface ITransientDependency;
   class TransientDependency : ITransientDependency;

   interface IServiceWithTransientDependency;
   class ServiceWithTransientDependency(ITransientDependency dependency) : IServiceWithTransientDependency
   {
      public ITransientDependency Dependency => dependency;
   }
#pragma warning restore CS9113 // Parameter is unread.
}
