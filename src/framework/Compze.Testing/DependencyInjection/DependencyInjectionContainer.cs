using System;
using System.Threading.Tasks;
using Compze.DependencyInjection.Microsoft;
using Compze.DependencyInjection.SimpleInjector;
using Compze.Messaging.Buses;
using Compze.Persistence.Common.DependencyInjection;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Testing;
using JetBrains.Annotations;

namespace Compze.DependencyInjection;

public static class DependencyInjectionContainer
{
   public static IServiceLocator CreateServiceLocatorForTesting([InstantHandle]Action<IEndpointBuilder> setup)
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
         DIContainer.Microsoft => new MicrosoftDependencyInjectionContainer(runMode),
         _ => throw new ArgumentOutOfRangeException()
      };

      container.Register(Singleton.For<IServiceLocator>().CreatedBy(() => container.ServiceLocator));
      return container;
   }

   class TestingEndpointHostDisposer(ITestingEndpointHost host) : IAsyncDisposable
   {
      readonly ITestingEndpointHost _host = host;
      public async ValueTask DisposeAsync() => await _host.DisposeAsync().CaF();
   }
}