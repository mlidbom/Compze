using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications.Transient_lifestyle;

public class When_resolving_an_untracked_transient
{
   [DependencyInjectionContainerMatrix]
   public void each_resolve_returns_a_new_instance()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();
      container.Register(UntrackedTransient.For<IMyService>().CreatedBy(() => new MyService()));

      var serviceLocator = container.ServiceLocator;
      var first = serviceLocator.Resolve<IMyService>();
      var second = serviceLocator.Resolve<IMyService>();

      first.Must().NotBe(second);
   }

   [DependencyInjectionContainerMatrix]
   public void disposable_instances_are_not_disposed_by_the_container()
   {
      var instance = default(DisposableService);

      {
         var container = DependencyInjectionContainerFactory.CreateContainer();
         container.Register(UntrackedTransient.For<IDisposableService>().CreatedBy(() => new DisposableService()));

         var serviceLocator = container.ServiceLocator;
         instance = (DisposableService)serviceLocator.Resolve<IDisposableService>();

         instance.IsDisposed.Must().BeFalse();

         container.Dispose();
      }

      instance!.IsDisposed.Must().BeFalse();
   }

   [DependencyInjectionContainerMatrix]
   public void disposable_instances_within_a_scope_are_not_disposed_when_scope_is_disposed()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();
      container.Register(UntrackedTransient.For<IDisposableService>().CreatedBy(() => new DisposableService()));

      var serviceLocator = container.ServiceLocator;

      DisposableService instance;

      using(serviceLocator.BeginScope())
      {
         instance = (DisposableService)serviceLocator.Resolve<IDisposableService>();
         instance.IsDisposed.Must().BeFalse();
      }

      instance.IsDisposed.Must().BeFalse();
   }

   [DependencyInjectionContainerMatrix]
   public void untracked_transient_can_depend_on_a_singleton()
   {
      using var container = DependencyInjectionContainerFactory.CreateContainer();
      container.Register(
         Singleton.For<ISingletonDependency>().CreatedBy(() => new SingletonDependency()),
         UntrackedTransient.For<IServiceWithDependency>().CreatedBy((ISingletonDependency dep) => new ServiceWithDependency(dep))
      );

      var serviceLocator = container.ServiceLocator;
      var first = serviceLocator.Resolve<IServiceWithDependency>();
      var second = serviceLocator.Resolve<IServiceWithDependency>();

      first.Must().NotBe(second);
      ((ServiceWithDependency)first).Dependency.Must().Be(((ServiceWithDependency)second).Dependency);
   }

   interface IMyService;
   class MyService : IMyService;

   interface IDisposableService;
   class DisposableService : IDisposableService, IDisposable
   {
      public bool IsDisposed { get; set; }
      public void Dispose() => IsDisposed = true;
   }

   interface ISingletonDependency;
   class SingletonDependency : ISingletonDependency;

   interface IServiceWithDependency;
   class ServiceWithDependency(ISingletonDependency dependency) : IServiceWithDependency
   {
      readonly ISingletonDependency _dependency = dependency;
      public ISingletonDependency Dependency => _dependency;
   }
}
