using System;
using System.Threading.Tasks;
using Composable.DependencyInjection.Microsoft;
using Composable.DependencyInjection.SimpleInjector;
using Composable.Messaging.Buses;
using Composable.Persistence.Common.DependencyInjection;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Composable.Testing;
using JetBrains.Annotations;

namespace Composable.DependencyInjection;

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
         DIContainer.Composable => new ComposableDependencyInjectionContainer(runMode),
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