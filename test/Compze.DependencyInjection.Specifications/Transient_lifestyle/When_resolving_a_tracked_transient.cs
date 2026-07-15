using Compze.DependencyInjection.Specifications.Infrastructure;
using Compze.Must;


// ReSharper disable InconsistentNaming

namespace Compze.DependencyInjection.Specifications.Transient_lifestyle;

public class When_resolving_a_tracked_transient
{
   public class without_a_scope : When_resolving_a_tracked_transient
   {
      [DependencyInjectionContainerMatrix]
      public void each_resolve_returns_a_new_instance()
      {
         var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
         builder.Registrar.Register(TrackedTransient.For<IMyService>().CreatedBy(() => new MyService()));

         using var container = builder.Build();
         var first = container.Resolve<IMyService>();
         var second = container.Resolve<IMyService>();

         first.Must().NotBe(second);
      }

      [DependencyInjectionContainerMatrix]
      public void disposable_instances_are_disposed_when_container_is_disposed()
      {
         DisposableService instance1;
         DisposableService instance2;

         {
            var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
            builder.Registrar.Register(TrackedTransient.For<IDisposableService>().CreatedBy(() => new DisposableService()));

            var container = builder.Build();
            instance1 = (DisposableService)container.Resolve<IDisposableService>();
            instance2 = (DisposableService)container.Resolve<IDisposableService>();

            instance1.IsDisposed.Must().BeFalse();
            instance2.IsDisposed.Must().BeFalse();

            container.Dispose();
         }

         instance1.IsDisposed.Must().BeTrue();
         instance2.IsDisposed.Must().BeTrue();
      }

      [DependencyInjectionContainerMatrix]
      public void async_disposable_only_instances_are_disposed_when_container_is_disposed()
      {
         AsyncOnlyDisposableService instance1;
         AsyncOnlyDisposableService instance2;

         {
            var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
            builder.Registrar.Register(TrackedTransient.For<IAsyncOnlyDisposableService>().CreatedBy(() => new AsyncOnlyDisposableService()));

            var container = builder.Build();
            instance1 = (AsyncOnlyDisposableService)container.Resolve<IAsyncOnlyDisposableService>();
            instance2 = (AsyncOnlyDisposableService)container.Resolve<IAsyncOnlyDisposableService>();

            instance1.IsDisposed.Must().BeFalse();
            instance2.IsDisposed.Must().BeFalse();

            container.Dispose();
         }

         instance1.IsDisposed.Must().BeTrue();
         instance2.IsDisposed.Must().BeTrue();
      }

      [DependencyInjectionContainerMatrix]
      public async Task disposable_instances_are_disposed_when_container_is_disposed_async()
      {
         DisposableService instance1;
         DisposableService instance2;

         {
            var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
            builder.Registrar.Register(TrackedTransient.For<IDisposableService>().CreatedBy(() => new DisposableService()));

            var container = builder.Build();
            instance1 = (DisposableService)container.Resolve<IDisposableService>();
            instance2 = (DisposableService)container.Resolve<IDisposableService>();

            instance1.IsDisposed.Must().BeFalse();
            instance2.IsDisposed.Must().BeFalse();

            await container.DisposeAsync();
         }

         instance1.IsDisposed.Must().BeTrue();
         instance2.IsDisposed.Must().BeTrue();
      }

      [DependencyInjectionContainerMatrix]
      public async Task async_disposable_only_instances_are_disposed_when_container_is_disposed_async()
      {
         AsyncOnlyDisposableService instance1;
         AsyncOnlyDisposableService instance2;

         {
            var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
            builder.Registrar.Register(TrackedTransient.For<IAsyncOnlyDisposableService>().CreatedBy(() => new AsyncOnlyDisposableService()));

            var container = builder.Build();
            instance1 = (AsyncOnlyDisposableService)container.Resolve<IAsyncOnlyDisposableService>();
            instance2 = (AsyncOnlyDisposableService)container.Resolve<IAsyncOnlyDisposableService>();

            instance1.IsDisposed.Must().BeFalse();
            instance2.IsDisposed.Must().BeFalse();

            await container.DisposeAsync();
         }

         instance1.IsDisposed.Must().BeTrue();
         instance2.IsDisposed.Must().BeTrue();
      }
   }

   public class within_a_scope : When_resolving_a_tracked_transient
   {
      [DependencyInjectionContainerMatrix]
      public void each_resolve_returns_a_new_instance()
      {
         var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
         builder.Registrar.Register(TrackedTransient.For<IMyService>().CreatedBy(() => new MyService()));

         using var container = builder.Build();
         using var scope = container.BeginScope();
         var first = scope.Resolve<IMyService>();
         var second = scope.Resolve<IMyService>();

         first.Must().NotBe(second);
      }

      [DependencyInjectionContainerMatrix]
      public void disposable_instances_are_disposed_when_scope_is_disposed()
      {
         var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
         builder.Registrar.Register(TrackedTransient.For<IDisposableService>().CreatedBy(() => new DisposableService()));

         using var container = builder.Build();

         DisposableService instance1;
         DisposableService instance2;

         {
            using var scope = container.BeginScope();
            instance1 = (DisposableService)scope.Resolve<IDisposableService>();
            instance2 = (DisposableService)scope.Resolve<IDisposableService>();

            instance1.IsDisposed.Must().BeFalse();
            instance2.IsDisposed.Must().BeFalse();
         }

         instance1.IsDisposed.Must().BeTrue();
         instance2.IsDisposed.Must().BeTrue();
      }

      [DependencyInjectionContainerMatrix]
      public void async_disposable_only_instances_are_disposed_when_scope_is_disposed()
      {
         var builder = DependencyInjectionContainerFactory.CreateContainerBuilder();
         builder.Registrar.Register(TrackedTransient.For<IAsyncOnlyDisposableService>().CreatedBy(() => new AsyncOnlyDisposableService()));

         using var container = builder.Build();

         AsyncOnlyDisposableService instance1;
         AsyncOnlyDisposableService instance2;

         {
            using var scope = container.BeginScope();
            instance1 = (AsyncOnlyDisposableService)scope.Resolve<IAsyncOnlyDisposableService>();
            instance2 = (AsyncOnlyDisposableService)scope.Resolve<IAsyncOnlyDisposableService>();

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
      public bool IsDisposed { get; private set; }
      public void Dispose() => IsDisposed = true;
   }

   interface IAsyncOnlyDisposableService;
   class AsyncOnlyDisposableService : IAsyncOnlyDisposableService, IAsyncDisposable
   {
      public bool IsDisposed { get; private set; }
      public ValueTask DisposeAsync()
      {
         IsDisposed = true;
         return ValueTask.CompletedTask;
      }
   }
}
