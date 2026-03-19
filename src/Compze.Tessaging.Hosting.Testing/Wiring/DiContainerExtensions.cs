using Compze.DocumentDb.Wiring;
using Compze.Abstractions.Refactoring.Naming.Internal.Implementation;
using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Teventive.TeventStore.Wiring;
using Compze.Typermedia.HandlerRegistration;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection.Autofac;
using Compze.DependencyInjection.Microsoft;
using Compze.Underscore;
using Compze.Internals.Logging;
using JetBrains.Annotations;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class DiContainerExtensions
{
   public static IContainerBuilder CreateWithContainerRegistrations(this DIContainer @this) =>
#pragma warning disable CA2000//We are passing this disposable out of the method
      @this.CreateEmpty();
#pragma warning restore CA2000

   public static IContainerBuilder CreateWithContainerRegistrationsAndCurrentTestsPluggableComponents(this DIContainer @this) =>
#pragma warning disable CA2000//We are passing it out of the method
      @this.CreateWithCurrentTestsPluggableComponents();
#pragma warning restore CA2000

   public static IContainerBuilder CreateWithCurrentTestsPluggableComponents(this DIContainer @this) =>
#pragma warning disable CA2000//We are passing this disposable out of the method
      @this.CreateEmpty()
           ._mutate(it => it.Registrar
                           .CurrentTestsPluggableComponents());
#pragma warning restore CA2000

   public static IContainerBuilder CreateEmpty(this DIContainer @this) =>
      @this switch
      {
         DIContainer.Microsoft      => new MicrosoftDependencyInjectionContainer(new TestingComponentRegistrar()),
         DIContainer.Autofac        => new AutofacDependencyInjectionContainer(new TestingComponentRegistrar()),
         _                          => throw new ArgumentOutOfRangeException()
      };

   public static IDependencyInjectionContainer CreateContainerForTesting(this DIContainer @this, [InstantHandle] Action<IComponentRegistrar> setup)
   {
#pragma warning disable CA2000//it is disposed with the container
      var builder = @this.CreateWithContainerRegistrationsAndCurrentTestsPluggableComponents();
#pragma warning restore CA2000
      builder.Registrar
               .TypeMapper()
               .DummyConfigurationParameterProvider()
               .TessageHandlerRegistry()
               .TypermediaHandlerRegistry()
               .InMemoryTeventStoreTeventPublisher();
      setup(builder.Registrar);

      return builder.Build();
   }

   public const string TeventStoreConnectionStringName = "Fake_connectionstring_for_database_testing";

   public static IDependencyInjectionContainer SetupTestingContainer(this DIContainer @this, [InstantHandle] Action<IComponentRegistrar>? configureContainer = null) =>
      CompzeLogger.For(typeof(DiContainerExtensions)).ExceptionsAndRethrow(() =>
                                                                              @this.CreateContainerForTesting(register =>
                                                                              {
                                                                                 register.DocumentDb();
                                                                                 register.TeventStore(TeventStoreConnectionStringName);
                                                                                 configureContainer?.Invoke(register);
                                                                              }));
}
