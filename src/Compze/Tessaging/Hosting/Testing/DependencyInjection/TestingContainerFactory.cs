using System;
using System.Threading.Tasks;
using Compze.Common.Refactoring.Naming.Wiring;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Configuration;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Tessaging.Hosting.Testing.Sql;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.DependencyInjection.Microsoft;
using Compze.Utilities.DependencyInjection.SimpleInjector;
using Compze.Wiring;
using JetBrains.Annotations;

namespace Compze.Tessaging.Hosting.Testing.DependencyInjection;

public static class TestingContainerFactory
{
   public static IServiceLocator CreateServiceLocatorForTesting([InstantHandle] Action<IDependencyRegistrar> setup)
   {
      var container = Create(RunMode.Testing);
      container.Register()
               .TimeSource()
               .TypeMapper()
               .DummyConfigurationParameterProvider()
               .CurrentTestsConfiguredSqlLayer()
               .MessageHandlerRegistry()
               .InMemoryEventStoreEventPublisher();
      setup(container.Register());

      return container.ServiceLocator;
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

static class DummyConfigurationParameterProviderRegistrar
{
   internal static IDependencyRegistrar DummyConfigurationParameterProvider(this IDependencyRegistrar registrar)
      => registrar.Register(Singleton.For<IConfigurationParameterProvider>()
                                     .CreatedBy(() => new DummyConfigurationParameterProvider()));
}

class DummyConfigurationParameterProvider : IConfigurationParameterProvider
{
   public string GetString(string parameterName, string? valueIfMissing = null) => throw new NotImplementedException();
}
