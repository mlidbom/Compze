using System;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Testing.Persistence;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Microsoft;
using Compze.Utilities.DependencyInjection.SimpleInjector;
using JetBrains.Annotations;

namespace Compze.Tessaging.Hosting.Testing.DependencyInjection;

public static class TestingContainerFactory
{
   public static IServiceLocator CreateServiceLocatorForTesting([InstantHandle] Action<IEndpointBuilder> setup)
   {
      var host = TestingEndpointHost.Create(Create);
      var endpoint = host.RegisterTestingEndpoint(setup: builder =>
      {
         builder.RegisterCurrentTestsConfiguredPersistenceLayer();
         setup(builder);
         //Hack to get the host to be disposed by the container when the container is disposed.
         builder.Container.Register(Singleton.For<TestingEndpointHostDisposer>().CreatedBy(() => new TestingEndpointHostDisposer(host)).DelegateToParentServiceLocatorWhenCloning());
      });

      return endpoint.ServiceLocator;
   }

   public static IDependencyInjectionContainer Create(IRunMode runMode)
   {
      IDependencyInjectionContainer container = TestEnv.DIContainer.Current switch
      {
         DIContainer.SimpleInjector => new SimpleInjectorDependencyInjectionContainer(runMode),
         DIContainer.Microsoft      => new MicrosoftDependencyInjectionContainer(runMode),
         _                          => throw new ArgumentOutOfRangeException()
      };

      container.Register(Singleton.For<IServiceLocator>().CreatedBy(() => container.ServiceLocator));
      return container;
   }

   class TestingEndpointHostDisposer(ITestingEndpointHost host) : IAsyncDisposable
   {
      readonly ITestingEndpointHost _host = host;
      public async ValueTask DisposeAsync() => await _host.DisposeAsync();
   }
}
