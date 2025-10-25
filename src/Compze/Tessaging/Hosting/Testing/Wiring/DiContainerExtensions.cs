using System;
using Compze.Abstractions.Time.Public;
using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.Common.Refactoring.Naming;
using Compze.Sql.DocumentDb.Wiring;
using Compze.Tessaging.Hosting.Testing.Sql;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Teventive.TeventStore.DependencyInjection;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.DependencyInjection.Microsoft;
using Compze.Utilities.DependencyInjection.SimpleInjector;
using Compze.Utilities.Logging;
using JetBrains.Annotations;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class DiContainerExtensions
{
   public static IDependencyInjectionContainer CreateWithRegisteredServiceLocator(this DIContainer @this)
   {
      var container = @this.Create();
      container.Register(Singleton.For<IServiceLocator>().CreatedBy(() => container.ServiceLocator));
      return container;
   }

   public static IDependencyInjectionContainer Create(this DIContainer @this)
   {
      IDependencyInjectionContainer container = @this switch
      {
         DIContainer.SimpleInjector => new SimpleInjectorDependencyInjectionContainer(new TestingComponentRegistrar()),
         DIContainer.Microsoft      => new MicrosoftDependencyInjectionContainer(new TestingComponentRegistrar()),
         _                          => throw new ArgumentOutOfRangeException()
      };
      return container;
   }

   public static IServiceLocator CreateServiceLocatorForTesting(this DIContainer @this, [InstantHandle] Action<IComponentRegistrar> setup)
   {
      var container = @this.CreateWithRegisteredServiceLocator();
      container.Register()
               .TimeSource()
               .TypeMapper()
               .DummyConfigurationParameterProvider()
               .CurrentTestsConfiguredSqlLayer()
               .TessageHandlerRegistry()
               .InMemoryTeventStoreTeventPublisher();
      setup(container.Register());

      return container.ServiceLocator;
   }

   public const string TeventStoreConnectionStringName = "Fake_connectionstring_for_database_testing";

   public static IServiceLocator SetupTestingServiceLocator(this DIContainer @this, [InstantHandle] Action<IComponentRegistrar>? configureContainer = null) =>
      CompzeLogger.For(typeof(DiContainerExtensions)).ExceptionsAndRethrow(() =>
                                                                              @this.CreateServiceLocatorForTesting(register =>
                                                                              {
                                                                                 register.DocumentDb();
                                                                                 register.TeventStore(TeventStoreConnectionStringName);
                                                                                 configureContainer?.Invoke(register);
                                                                              }));
}
