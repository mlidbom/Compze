using System;
using Compze.Common.Refactoring.Naming.Wiring;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Tessaging.Hosting.Testing.Sql;
using Compze.Tessaging.Teventive.EventStore.DependencyInjection;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.DependencyInjection.Microsoft;
using Compze.Utilities.DependencyInjection.SimpleInjector;
using Compze.Utilities.Logging;
using Compze.Wiring;
using JetBrains.Annotations;
using Compze.Sql.DocumentDb.DependencyInjection;

namespace Compze.Tessaging.Hosting.Testing.DependencyInjection;

public static class DiContainerExtensions
{
   public static IDependencyInjectionContainer Create(this DIContainer @this, IRunMode? runMode = null)
   {
      runMode = runMode ?? RunMode.Testing;
      IDependencyInjectionContainer container = @this switch
      {
         DIContainer.SimpleInjector => new SimpleInjectorDependencyInjectionContainer(runMode),
         DIContainer.Microsoft      => new MicrosoftDependencyInjectionContainer(runMode),
         _                          => throw new ArgumentOutOfRangeException()
      };

      container.Register(Singleton.For<IServiceLocator>().CreatedBy(() => container.ServiceLocator));
      return container;
   }

   public static IServiceLocator CreateServiceLocatorForTesting(this DIContainer @this, [InstantHandle] Action<IDependencyRegistrar> setup)
   {
      var container = @this.Create(RunMode.Testing);
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

   public const string EventStoreConnectionStringName = "Fake_connectionstring_for_database_testing";

   public static IServiceLocator SetupTestingServiceLocator(this DIContainer @this, [InstantHandle] Action<IDependencyRegistrar>? configureContainer = null) =>
      CompzeLogger.For(typeof(DiContainerExtensions)).ExceptionsAndRethrow(() =>
                                                                              @this.CreateServiceLocatorForTesting(register =>
                                                                              {
                                                                                 register.DocumentDb();
                                                                                 register.EventStore(EventStoreConnectionStringName);
                                                                                 configureContainer?.Invoke(register);
                                                                              }));
}
