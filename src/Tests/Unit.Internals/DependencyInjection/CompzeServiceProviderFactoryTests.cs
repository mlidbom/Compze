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
/// Tests for CompzeServiceProviderFactory to verify it correctly integrates
/// Compze's DI container with Microsoft's DI without double-disposal.
/// </summary>
[TestFixture]
public class CompzeServiceProviderFactoryTests : UniversalTestBase
{
   class DisposableService : IDisposable
   {
      public int DisposeCallCount { get; private set; }
      public bool IsDisposed => DisposeCallCount > 0;

      public void Dispose()
      {
         DisposeCallCount++;
         GC.SuppressFinalize(this);
      }
   }

   interface IMyService
   {
      string GetValue();
   }

   class MyService : IMyService
   {
      public string GetValue() => "From Compze Container";
   }

   class MicrosoftService
   {
      public string GetValue() => "From Microsoft Container";
   }

   [Test]
   public void Factory_Creates_HybridServiceProvider()
   {
      // Arrange
      IDependencyInjectionContainer container = new MicrosoftDependencyInjectionContainer(RunMode.Production);
      var factory = container.CreateServiceProviderFactory();
      var services = new ServiceCollection();

      // Act
      var builder = factory.CreateBuilder(services);
      var provider = factory.CreateServiceProvider(builder);

      // Assert
      Assert.That(provider, Is.Not.Null);
      Assert.That(provider, Is.InstanceOf<IServiceProvider>());
   }

   [Test]
   public void HybridProvider_Resolves_From_Compze_Container_First()
   {
      // Arrange
      IDependencyInjectionContainer container = new MicrosoftDependencyInjectionContainer(RunMode.Production);
      container.Register(
         Singleton.For<IMyService>().CreatedBy(() => new MyService())
      );

      var factory = container.CreateServiceProviderFactory();
      var services = new ServiceCollection();

      // Act
      var provider = factory.CreateServiceProvider(services);
      var service = provider.GetService<IMyService>();

      // Assert
      Assert.That(service, Is.Not.Null);
      Assert.That(service!.GetValue(), Is.EqualTo("From Compze Container"));
   }

   [Test]
   public void HybridProvider_Falls_Back_To_Microsoft_Container()
   {
      // Arrange
      IDependencyInjectionContainer container = new MicrosoftDependencyInjectionContainer(RunMode.Production);
      var factory = container.CreateServiceProviderFactory();
      
      var services = new ServiceCollection();
      services.AddSingleton<MicrosoftService>();

      // Act
      var provider = factory.CreateServiceProvider(services);
      var service = provider.GetService<MicrosoftService>();

      // Assert
      Assert.That(service, Is.Not.Null);
      Assert.That(service!.GetValue(), Is.EqualTo("From Microsoft Container"));
   }

   [Test]
   public void HybridProvider_Does_NOT_Double_Dispose_Compze_Singletons()
   {
      // Arrange
      IDependencyInjectionContainer container = new MicrosoftDependencyInjectionContainer(RunMode.Production);
      DisposableService? trackedService = null;

      container.Register(
         Singleton.For<DisposableService>()
                  .CreatedBy(() =>
                  {
                     var service = new DisposableService();
                     trackedService = service;
                     return service;
                  })
      );

      var factory = container.CreateServiceProviderFactory();
      var services = new ServiceCollection();
      var provider = factory.CreateServiceProvider(services);

      // Act - Resolve from hybrid provider
      var service = provider.GetRequiredService<DisposableService>();
      Assert.That(service, Is.SameAs(trackedService));
      Assert.That(service.IsDisposed, Is.False);

      // Dispose Microsoft's provider
      (provider as IDisposable)?.Dispose();

      // Assert - Should NOT be disposed by Microsoft
      Assert.That(service.DisposeCallCount, Is.EqualTo(0),
         "Microsoft provider should NOT dispose services from Compze container");

      // Dispose Compze container
      container.Dispose();

      // Assert - Should be disposed ONCE by Compze container
      Assert.That(service.DisposeCallCount, Is.EqualTo(1),
         "Compze container should dispose the service once");
   }

   [Test]
   public void HybridProvider_Does_NOT_Double_Dispose_Compze_Scoped_Services()
   {
      // Arrange
      IDependencyInjectionContainer container = new MicrosoftDependencyInjectionContainer(RunMode.Production);
      DisposableService? trackedService = null;

      container.Register(
         Scoped.For<DisposableService>()
               .CreatedBy(() =>
               {
                  var service = new DisposableService();
                  trackedService = service;
                  return service;
               })
      );

      var factory = container.CreateServiceProviderFactory();
      var services = new ServiceCollection();
      var provider = factory.CreateServiceProvider(services);

      DisposableService service;

      // Act - Create scope through the hybrid provider (which manages both scopes internally)
      var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
      using (var microsoftScope = scopeFactory.CreateScope())
      {
         service = microsoftScope.ServiceProvider.GetRequiredService<DisposableService>();
         Assert.That(service, Is.SameAs(trackedService));
         Assert.That(service.IsDisposed, Is.False);
      }

      // Assert - Disposed once by the hybrid scope
      Assert.That(service.DisposeCallCount, Is.EqualTo(1),
         "Should be disposed once by the hybrid scope");

      // Cleanup
      (provider as IDisposable)?.Dispose();
      container.Dispose();
      
      // Verify no additional disposals
      Assert.That(service.DisposeCallCount, Is.EqualTo(1),
         "Should still be disposed only once");
   }

   [Test]
   public async Task HybridProvider_Does_NOT_Double_AsyncDispose_Compze_Services()
   {
      // Arrange
      IDependencyInjectionContainer container = new MicrosoftDependencyInjectionContainer(RunMode.Production);
      DisposableService? trackedService = null;

      container.Register(
         Singleton.For<DisposableService>()
                  .CreatedBy(() =>
                  {
                     var service = new DisposableService();
                     trackedService = service;
                     return service;
                  })
      );

      var factory = container.CreateServiceProviderFactory();
      var services = new ServiceCollection();
      var provider = factory.CreateServiceProvider(services);

      // Act - Resolve from hybrid provider
      var service = provider.GetRequiredService<DisposableService>();
      Assert.That(service, Is.SameAs(trackedService));
      Assert.That(service.IsDisposed, Is.False);

      // Dispose Microsoft's provider asynchronously
      if (provider is IAsyncDisposable asyncDisposable)
      {
         await asyncDisposable.DisposeAsync();
      }

      // Assert - Should NOT be disposed by Microsoft
      Assert.That(service.DisposeCallCount, Is.EqualTo(0),
         "Microsoft provider should NOT dispose services from Compze container");

      // Dispose Compze container
      await container.DisposeAsync();

      // Assert - Should be disposed ONCE by Compze container
      Assert.That(service.DisposeCallCount, Is.EqualTo(1),
         "Compze container should dispose the service once");
   }

   [Test]
   public void HybridProvider_Returns_Itself_For_IServiceProvider()
   {
      // Arrange
      IDependencyInjectionContainer container = new MicrosoftDependencyInjectionContainer(RunMode.Production);
      var factory = container.CreateServiceProviderFactory();
      var services = new ServiceCollection();
      var provider = factory.CreateServiceProvider(services);

      // Act
      var resolvedProvider = provider.GetService<IServiceProvider>();

      // Assert
      Assert.That(resolvedProvider, Is.SameAs(provider),
         "Should return itself when resolving IServiceProvider");
   }

   [Test]
   public void HybridProvider_Returns_Itself_For_IServiceScopeFactory()
   {
      // Arrange
      IDependencyInjectionContainer container = new MicrosoftDependencyInjectionContainer(RunMode.Production);
      var factory = container.CreateServiceProviderFactory();
      var services = new ServiceCollection();
      var provider = factory.CreateServiceProvider(services);

      // Act
      var scopeFactory = provider.GetService<IServiceScopeFactory>();

      // Assert
      Assert.That(scopeFactory, Is.SameAs(provider),
         "Should return itself when resolving IServiceScopeFactory");
   }

   [Test]
   public void Microsoft_Services_ARE_Disposed_By_Microsoft_Provider()
   {
      // Arrange
      IDependencyInjectionContainer container = new MicrosoftDependencyInjectionContainer(RunMode.Production);
      var factory = container.CreateServiceProviderFactory();
      
      var services = new ServiceCollection();
      services.AddSingleton<DisposableService>();
      
      var provider = factory.CreateServiceProvider(services);

      // Act - Resolve Microsoft service
      var service = provider.GetRequiredService<DisposableService>();
      Assert.That(service.IsDisposed, Is.False);

      // Dispose Microsoft's provider
      (provider as IDisposable)?.Dispose();

      // Assert - Microsoft services should be disposed by Microsoft
      Assert.That(service.DisposeCallCount, Is.EqualTo(1),
         "Microsoft provider should dispose its own services");

      // Cleanup - Compze container should not affect Microsoft services
      container.Dispose();
      Assert.That(service.DisposeCallCount, Is.EqualTo(1),
         "Compze container should not dispose Microsoft services");
   }

   [Test]
   public void CreateBuilder_Returns_Same_ServiceCollection()
   {
      // Arrange
      IDependencyInjectionContainer container = new MicrosoftDependencyInjectionContainer(RunMode.Production);
      var factory = container.CreateServiceProviderFactory();
      var services = new ServiceCollection();
      services.AddSingleton<MicrosoftService>();

      // Act
      var builder = factory.CreateBuilder(services);

      // Assert
      Assert.That(builder, Is.SameAs(services),
         "Should return the same IServiceCollection instance");
      Assert.That(builder.Count, Is.EqualTo(1),
         "Should preserve existing service registrations");
   }
}
