using System;
using System.Threading.Tasks;
using Compze.Testing;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using MicrosoftDI = Compze.Utilities.DependencyInjection.Microsoft;

namespace Compze.Tests.Unit.Internals.DependencyInjection;

/// <summary>
/// Tests to verify whether Microsoft's DI container disposes transient components
/// and whether this causes double-disposal when using RegisterToHandleServiceResolutionFor.
/// 
/// According to Microsoft docs: "The container is responsible for cleanup of types it creates,
/// and calls Dispose on IDisposable instances. Services resolved from the container should
/// never be disposed by the developer. If a type or factory is registered as a singleton,
/// the container disposes the singleton automatically."
/// 
/// This means transient services resolved from IServiceProvider ARE tracked and disposed.
/// </summary>
[TestFixture]
public class MicrosoftDependencyInjectionTransientDisposalTests : UniversalTestBase
{
   class DisposableComponent : IDisposable
   {
      public int DisposeCallCount { get; private set; }
      public bool IsDisposed => DisposeCallCount > 0;

      public void Dispose()
      {
         DisposeCallCount++;
         GC.SuppressFinalize(this);
      }
   }

   class AsyncDisposableComponent : IAsyncDisposable
   {
      public int DisposeCallCount { get; private set; }
      public bool IsDisposed => DisposeCallCount > 0;

      public async ValueTask DisposeAsync()
      {
         DisposeCallCount++;
         await Task.CompletedTask;
         GC.SuppressFinalize(this);
      }
   }

   class BothDisposableComponent : IDisposable, IAsyncDisposable
   {
      public int SyncDisposeCallCount { get; private set; }
      public int AsyncDisposeCallCount { get; private set; }
      public bool IsDisposed => SyncDisposeCallCount > 0 || AsyncDisposeCallCount > 0;

      public void Dispose()
      {
         SyncDisposeCallCount++;
         GC.SuppressFinalize(this);
      }

      public async ValueTask DisposeAsync()
      {
         AsyncDisposeCallCount++;
         await Task.CompletedTask;
         GC.SuppressFinalize(this);
      }
   }

   [Test]
   public void Microsoft_DI_Container_DOES_Dispose_Transient_Components()
   {
      // This test verifies the "insane" behavior that Microsoft DI tracks and disposes transient components
      var services = new ServiceCollection();
      DisposableComponent? resolvedComponent = null;

      services.AddTransient(_ =>
      {
         var component = new DisposableComponent();
         resolvedComponent = component;
         return component;
      });

      var provider = services.BuildServiceProvider();

      var component = provider.GetRequiredService<DisposableComponent>();
      Assert.That(component, Is.SameAs(resolvedComponent));
      Assert.That(component.IsDisposed, Is.False, "Component should not be disposed yet");

      provider.Dispose();

      Assert.That(component.IsDisposed, Is.True, "Microsoft DI container DOES dispose transient components!");
      Assert.That(component.DisposeCallCount, Is.EqualTo(1), "Component should be disposed exactly once by Microsoft container");
   }

   [Test]
   public async Task Microsoft_DI_Container_DOES_AsyncDispose_Transient_Components()
   {
      var services = new ServiceCollection();
      AsyncDisposableComponent? resolvedComponent = null;

      services.AddTransient(_ =>
      {
         var component = new AsyncDisposableComponent();
         resolvedComponent = component;
         return component;
      });

      var provider = services.BuildServiceProvider();

      var component = provider.GetRequiredService<AsyncDisposableComponent>();
      Assert.That(component, Is.SameAs(resolvedComponent));
      Assert.That(component.IsDisposed, Is.False, "Component should not be disposed yet");

      await provider.DisposeAsync();

      Assert.That(component.IsDisposed, Is.True, "Microsoft DI container DOES async dispose transient components!");
      Assert.That(component.DisposeCallCount, Is.EqualTo(1), "Component should be disposed exactly once by Microsoft container");
   }

   [Test]
   public void RegisterToHandleServiceResolutionFor_With_Scoped_Components_Cannot_Be_Resolved_From_Root()
   {
      // This test demonstrates that scoped components registered via RegisterToHandleServiceResolutionFor
      // cannot be resolved from the root provider because the implementation delegates back to our container
      // which tries to resolve from the Microsoft root provider (not a scope)
      IDependencyInjectionContainer container = new MicrosoftDI.MicrosoftDependencyInjectionContainer(RunMode.Production);

      container.Register(
         Scoped.For<DisposableComponent>()
               .CreatedBy(() => new DisposableComponent())
      );

      var services = new ServiceCollection();
      container.RegisterToHandleServiceResolutionFor(services);

      var microsoftProvider = services.BuildServiceProvider();

      // This will fail because our implementation delegates to our container which tries to resolve
      // from the Microsoft root provider, and Microsoft DI doesn't allow resolving scoped services from root
      using var scope = microsoftProvider.CreateScope();
      
      Assert.Throws<InvalidOperationException>(() =>
         scope.ServiceProvider.GetRequiredService<DisposableComponent>(),
         "Cannot resolve scoped service from root provider - this is a limitation of the current implementation");
   }

   [Test]
   public async Task RegisterToHandleServiceResolutionFor_With_Scoped_Components_Cannot_Be_Resolved_Async()
   {
      // Same limitation as the sync version
      IDependencyInjectionContainer container = new MicrosoftDI.MicrosoftDependencyInjectionContainer(RunMode.Production);

      container.Register(
         Scoped.For<BothDisposableComponent>()
               .CreatedBy(() => new BothDisposableComponent())
      );

      var services = new ServiceCollection();
      container.RegisterToHandleServiceResolutionFor(services);

      var microsoftProvider = services.BuildServiceProvider();

      await using var scope = microsoftProvider.CreateAsyncScope();
      
      Assert.Throws<InvalidOperationException>(() =>
         scope.ServiceProvider.GetRequiredService<BothDisposableComponent>(),
         "Cannot resolve scoped service - this is a limitation of the current implementation");
   }

   [Test]
   public void Singleton_Instance_Registrations_ARE_ALSO_Disposed_By_Microsoft_DI()
   {
      // SURPRISING BEHAVIOR: Even singleton instance registrations are disposed by Microsoft DI!
      // This is contrary to typical DI container behavior
      IDependencyInjectionContainer container = new MicrosoftDI.MicrosoftDependencyInjectionContainer(RunMode.Production);
      var singletonInstance = new DisposableComponent();

      container.Register(
         Singleton.For<DisposableComponent>()
                  .Instance(singletonInstance)
      );

      var services = new ServiceCollection();
      container.RegisterToHandleServiceResolutionFor(services);

      var microsoftProvider = services.BuildServiceProvider();

      var component = microsoftProvider.GetRequiredService<DisposableComponent>();
      Assert.That(component, Is.SameAs(singletonInstance));
      Assert.That(component.IsDisposed, Is.False);

      // Dispose Microsoft provider
      microsoftProvider.Dispose();

      // Microsoft DI DOES dispose even instance registrations!
      Assert.That(component.DisposeCallCount, Is.EqualTo(1),
         "Microsoft DI disposes even components registered as instances!");

      // Our container should not dispose instance registrations
      container.Dispose();

      // Verify no additional disposal by our container
      Assert.That(component.DisposeCallCount, Is.EqualTo(1),
         "Our container correctly does not dispose instance registrations, but Microsoft DI already did!");
   }

   [Test]
   public void Factory_Created_Singletons_ARE_Double_Disposed()
   {
      // Singletons created by factory WILL have double-disposal issues
      IDependencyInjectionContainer container = new MicrosoftDI.MicrosoftDependencyInjectionContainer(RunMode.Production);
      DisposableComponent? trackedComponent = null;

      container.Register(
         Singleton.For<DisposableComponent>()
                  .CreatedBy(() =>
                  {
                     var component = new DisposableComponent();
                     trackedComponent = component;
                     return component;
                  })
      );

      var services = new ServiceCollection();
      container.RegisterToHandleServiceResolutionFor(services);

      var microsoftProvider = services.BuildServiceProvider();

      var component = microsoftProvider.GetRequiredService<DisposableComponent>();
      Assert.That(component, Is.SameAs(trackedComponent));
      Assert.That(component.IsDisposed, Is.False);

      microsoftProvider.Dispose();

      var disposeCountAfterMicrosoftDisposal = component.DisposeCallCount;
      Assert.That(disposeCountAfterMicrosoftDisposal, Is.GreaterThan(0),
         "Microsoft DI disposed the singleton created by factory");

      container.Dispose();

      Assert.That(component.DisposeCallCount, Is.GreaterThan(disposeCountAfterMicrosoftDisposal),
         "Singleton was disposed TWICE - once by Microsoft container, once by our container!");
   }
}
