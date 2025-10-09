using System;
using System.Threading.Tasks;
using Compze.Testing;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.DependencyInjection.Microsoft;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

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
   public void RegisterToHandleServiceResolutionFor_Scoped_Components_ARE_Double_Disposed()
   {
      // Scoped components ARE double-disposed! 
      // 1. Once when OUR container's scope is disposed
      // 2. Again when Microsoft's provider is disposed (because it tracks transients)
      IDependencyInjectionContainer container = new MicrosoftDependencyInjectionContainer(RunMode.Production);
      DisposableComponent? trackedComponent = null;

      container.Register(
         Scoped.For<DisposableComponent>()
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

      DisposableComponent component;
      // Create scope using OUR container's ServiceLocator
      using (container.ServiceLocator.BeginScope())
      {
         component = microsoftProvider.GetRequiredService<DisposableComponent>();
         Assert.That(component, Is.SameAs(trackedComponent));
         Assert.That(component.IsDisposed, Is.False, "Component should not be disposed yet");
      }

      // After scope disposal by OUR container, component should be disposed once
      Assert.That(component.DisposeCallCount, Is.EqualTo(1),
         "Component disposed once by our container's scope");

      // Dispose the Microsoft provider - DOES dispose again! (the double-disposal problem)
      microsoftProvider.Dispose();
      Assert.That(component.DisposeCallCount, Is.EqualTo(2),
         "Microsoft DI ALSO disposed the component - double disposal confirmed!");

      // Now dispose our container - should not dispose again (already disposed by both scope and Microsoft)
      container.Dispose();
      Assert.That(component.DisposeCallCount, Is.EqualTo(2),
         "Total of 2 disposals: one by our scope, one by Microsoft DI");
   }

   [Test]
   public async Task RegisterToHandleServiceResolutionFor_Scoped_Components_ARE_Double_AsyncDisposed()
   {
      // Same double-disposal issue with async disposal
      IDependencyInjectionContainer container = new MicrosoftDependencyInjectionContainer(RunMode.Production);
      BothDisposableComponent? trackedComponent = null;

      container.Register(
         Scoped.For<BothDisposableComponent>()
               .CreatedBy(() =>
               {
                  var component = new BothDisposableComponent();
                  trackedComponent = component;
                  return component;
               })
      );

      var services = new ServiceCollection();
      container.RegisterToHandleServiceResolutionFor(services);

      var microsoftProvider = services.BuildServiceProvider();

      BothDisposableComponent component;
      // Create scope using OUR container's ServiceLocator
      using (container.ServiceLocator.BeginScope())
      {
         component = microsoftProvider.GetRequiredService<BothDisposableComponent>();
         Assert.That(component, Is.SameAs(trackedComponent));
         Assert.That(component.IsDisposed, Is.False, "Component should not be disposed yet");
      }

      // After scope disposal by OUR container
      var totalDisposeCount = component.AsyncDisposeCallCount + component.SyncDisposeCallCount;
      Assert.That(totalDisposeCount, Is.EqualTo(1),
         "Component disposed once by our container's scope");

      // Dispose the Microsoft provider - DOES dispose again! (double-disposal problem)
      await microsoftProvider.DisposeAsync();
      var afterMicrosoftDisposal = component.AsyncDisposeCallCount + component.SyncDisposeCallCount;
      Assert.That(afterMicrosoftDisposal, Is.EqualTo(2),
         "Microsoft DI ALSO disposed the component - double disposal confirmed!");

      // Now dispose our container - should not dispose again
      await container.DisposeAsync();
      var finalDisposeCount = component.AsyncDisposeCallCount + component.SyncDisposeCallCount;
      Assert.That(finalDisposeCount, Is.EqualTo(2),
         "Total of 2 disposals: one by our scope, one by Microsoft DI");
   }

   [Test]
   public void Singleton_Instance_Registrations_ARE_ALSO_Disposed_By_Microsoft_DI()
   {
      // SURPRISING BEHAVIOR: Even singleton instance registrations are disposed by Microsoft DI!
      // This is contrary to typical DI container behavior
      IDependencyInjectionContainer container = new MicrosoftDependencyInjectionContainer(RunMode.Production);
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
      IDependencyInjectionContainer container = new MicrosoftDependencyInjectionContainer(RunMode.Production);
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
