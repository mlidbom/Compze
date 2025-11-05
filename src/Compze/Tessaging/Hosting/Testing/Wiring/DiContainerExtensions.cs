using System;
using Compze.Core.DocumentDb.Wiring;
using Compze.Core.Refactoring.Naming.Internal.Implementation;
using Compze.Core.Wiring.Testing.Internal;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Teventive.TeventStore.Wiring;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.DependencyInjection.Microsoft;
using Compze.Utilities.DependencyInjection.SimpleInjector;
using Compze.Utilities.Functional;
using Compze.Utilities.Logging;
using JetBrains.Annotations;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class DiContainerExtensions
{
   public static IDependencyInjectionContainer CreateWithServiceLocator(this DIContainer @this) =>
#pragma warning disable CA2000//We are passing this disposable out of the method
      @this.CreateEmpty()
           .mutate(it => it.Register(Singleton.For<IServiceLocator>()
                                              .CreatedBy(() => it.ServiceLocator)));
#pragma warning restore CA2000

   public static IDependencyInjectionContainer CreateWithServiceLocatorAndCurrentTestsPluggableComponents(this DIContainer @this) =>
#pragma warning disable CA2000//We are passing it out of the method
      @this.CreateWithCurrentTestsPluggableComponents()
           .mutate(it => it.Register(Singleton.For<IServiceLocator>()
                                              .CreatedBy(() => it.ServiceLocator)));
#pragma warning restore CA2000

   public static IDependencyInjectionContainer CreateWithCurrentTestsPluggableComponents(this DIContainer @this) =>
#pragma warning disable CA2000//We are passing this disposable out of the method
      @this.CreateEmpty()
           .mutate(it => it.Register()
                           .CurrentTestsPluggableComponents());
#pragma warning restore CA2000

   public static IDependencyInjectionContainer CreateEmpty(this DIContainer @this) =>
      @this switch
      {
         DIContainer.SimpleInjector => new SimpleInjectorDependencyInjectionContainer(new TestingComponentRegistrar()),
         DIContainer.Microsoft      => new MicrosoftDependencyInjectionContainer(new TestingComponentRegistrar()),
         _                          => throw new ArgumentOutOfRangeException()
      };

   public static IServiceLocator CreateServiceLocatorForTesting(this DIContainer @this, [InstantHandle] Action<IComponentRegistrar> setup)
   {
#pragma warning disable CA2000//it is disposed with the service locator
      var container = @this.CreateWithServiceLocatorAndCurrentTestsPluggableComponents();
#pragma warning restore CA2000
      container.Register()
               .TypeMapper()
               .DummyConfigurationParameterProvider()
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
