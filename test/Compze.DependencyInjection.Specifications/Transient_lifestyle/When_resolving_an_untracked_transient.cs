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

   interface IMyService;
   class MyService : IMyService;

   interface IDisposableService;
   class DisposableService : IDisposableService, IDisposable
   {
      public bool IsDisposed { get; set; }
      public void Dispose() => IsDisposed = true;
   }
}
