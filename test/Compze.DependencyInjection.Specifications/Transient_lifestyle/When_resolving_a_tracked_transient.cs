using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;

namespace Compze.DependencyInjection.Specifications;

public class When_resolving_a_tracked_transient
{
   public class without_a_scope : When_resolving_a_tracked_transient
   {
      [DependencyInjectionContainerMatrix]
      public void each_resolve_returns_a_new_instance()
      {
         using var container = DependencyInjectionContainerFactory.CreateContainer();
         container.Register(TrackedTransient.For<IMyService>().CreatedBy(() => new MyService()));

         var serviceLocator = container.ServiceLocator;
         var first = serviceLocator.Resolve<IMyService>();
         var second = serviceLocator.Resolve<IMyService>();

         first.Must().NotBe(second);
      }

      [DependencyInjectionContainerMatrix]
      public void disposable_instances_are_disposed_when_container_is_disposed()
      {
         var instance1 = default(DisposableService);
         var instance2 = default(DisposableService);

         {
            var container = DependencyInjectionContainerFactory.CreateContainer();
            container.Register(TrackedTransient.For<IDisposableService>().CreatedBy(() => new DisposableService()));

            var serviceLocator = container.ServiceLocator;
            instance1 = (DisposableService)serviceLocator.Resolve<IDisposableService>();
            instance2 = (DisposableService)serviceLocator.Resolve<IDisposableService>();

            instance1.IsDisposed.Must().BeFalse();
            instance2.IsDisposed.Must().BeFalse();

            container.Dispose();
         }

         instance1!.IsDisposed.Must().BeTrue();
         instance2!.IsDisposed.Must().BeTrue();
      }
   }

   public class within_a_scope : When_resolving_a_tracked_transient
   {
      [DependencyInjectionContainerMatrix]
      public void each_resolve_returns_a_new_instance()
      {
         using var container = DependencyInjectionContainerFactory.CreateContainer();
         container.Register(TrackedTransient.For<IMyService>().CreatedBy(() => new MyService()));

         var serviceLocator = container.ServiceLocator;
         using(serviceLocator.BeginScope())
         {
            var first = serviceLocator.Resolve<IMyService>();
            var second = serviceLocator.Resolve<IMyService>();

            first.Must().NotBe(second);
         }
      }

      [DependencyInjectionContainerMatrix]
      public void disposable_instances_are_disposed_when_scope_is_disposed()
      {
         using var container = DependencyInjectionContainerFactory.CreateContainer();
         container.Register(TrackedTransient.For<IDisposableService>().CreatedBy(() => new DisposableService()));

         var serviceLocator = container.ServiceLocator;

         DisposableService instance1;
         DisposableService instance2;

         using(serviceLocator.BeginScope())
         {
            instance1 = (DisposableService)serviceLocator.Resolve<IDisposableService>();
            instance2 = (DisposableService)serviceLocator.Resolve<IDisposableService>();

            instance1.IsDisposed.Must().BeFalse();
            instance2.IsDisposed.Must().BeFalse();
         }

         instance1.IsDisposed.Must().BeTrue();
         instance2.IsDisposed.Must().BeTrue();
      }
   }

   interface IMyService;
   class MyService : IMyService;

   interface IDisposableService;
   class DisposableService : IDisposableService, IDisposable
   {
      public bool IsDisposed { get; set; }
      public void Dispose() => IsDisposed = true;
   }
}
