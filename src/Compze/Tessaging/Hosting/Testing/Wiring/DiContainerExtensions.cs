using System;
using Compze.Core.Refactoring.Naming.Internal.Implementation;
using Compze.Core.Time.Public;
using Compze.Core.Wiring.Testing.Internal;
using Compze.DocumentDb.Wiring;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Teventive.TeventStore.Wiring;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.DependencyInjection.Microsoft;
using Compze.Utilities.DependencyInjection.SimpleInjector;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE;
using JetBrains.Annotations;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class DiContainerExtensions
{
   public static IDependencyInjectionContainer CreateWithServiceLocatorAndSerializer(this DIContainer @this)
   {
      var container = @this.CreateEmpty();
      container.Register().CastTo<TestingComponentRegistrar>()
               .CurrentTestsSerializer();
      container.Register(Singleton.For<IServiceLocator>().CreatedBy(() => container.ServiceLocator));
      return container;
   }

   public static IDependencyInjectionContainer CreateEmpty(this DIContainer @this) =>
      @this switch
      {
         DIContainer.SimpleInjector => new SimpleInjectorDependencyInjectionContainer(new TestingComponentRegistrar()),
         DIContainer.Microsoft      => new MicrosoftDependencyInjectionContainer(new TestingComponentRegistrar()),
         _                          => throw new ArgumentOutOfRangeException()
      };

   public static IServiceLocator CreateServiceLocatorForTesting(this DIContainer @this, [InstantHandle] Action<IComponentRegistrar> setup)
   {
      var container = @this.CreateWithServiceLocatorAndSerializer();
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
