using Compze.DocumentDb.Wiring;
using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.Hosting.Testing.Wiring;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tessaging.Hosting.Testing.Typermedia.Wiring;
using Compze.TypeIdentifiers;
using Compze.DependencyInjection.Abstractions;
using Compze.Underscore;
using Compze.Internals.Logging;
using Compze.Teventive.TeventStore.Wiring;
using JetBrains.Annotations;

namespace Compze.Tests.Common.Wiring;

///<summary>Container wiring for the combined test suite, whose endpoints and containers use both Tessaging and Typermedia and the full persistence stack.</summary>
public static class CombinedTestingContainers
{
   ///<summary>Registers everything a combined Tessaging+Typermedia test endpoint needs of the current test's pluggable components: both transports and the full SQL persistence stack, against a unique throwaway database.</summary>
   public static IComponentRegistrar CurrentTestsPluggableComponents(this IComponentRegistrar register) =>
      register.CurrentTestsPluggableComponents(Guid.NewGuid().ToString());

   ///<summary>Registers everything a combined Tessaging+Typermedia test endpoint needs of the current test's pluggable components: the endpoint transport and the full SQL persistence stack.<br/>
   /// (Each communication style's feature registers its own transport client and request handling on top of the endpoint transport.)</summary>
   public static IComponentRegistrar CurrentTestsPluggableComponents(this IComponentRegistrar register, string connectionStringName) =>
      register.CurrentTestsEndpointTransport()
              .CurrentTestsConfiguredSqlLayer(connectionStringName);

   public static IContainerBuilder CreateWithCurrentTestsPluggableComponents(this DIContainer @this) =>
      @this.CreateTestingContainerBuilder()
           ._mutate(it => it.Registrar.CurrentTestsPluggableComponents());

   public static IDependencyInjectionContainer CreateContainerForTesting(this DIContainer @this, Action<ITypeMapper> registerDomainTypeMappings, [InstantHandle] Action<IComponentRegistrar> setup, Action<LocalTessagingEngineBuilder>? composeEngine = null)
   {
      var builder = @this.CreateWithCurrentTestsPluggableComponents();
      builder.Registrar
               .TypeIdentifierMapper(registerDomainTypeMappings)
               .DummyConfigurationParameterProvider()
               .LocalTessagingEngine(composeEngine ?? (engine => {}));
      setup(builder.Registrar);

      return builder.Build();
   }

   public const string TeventStoreConnectionStringName = "Fake_connectionstring_for_database_testing";

   public static IDependencyInjectionContainer SetupTestingContainer(this DIContainer @this, Action<ITypeMapper> registerDomainTypeMappings, [InstantHandle] Action<IComponentRegistrar>? configureContainer = null, Action<LocalTessagingEngineBuilder>? composeEngine = null) =>
      CompzeLogger.For(typeof(CombinedTestingContainers)).ExceptionsAndRethrow(() =>
                                                                              @this.CreateContainerForTesting(registerDomainTypeMappings, register =>
                                                                              {
                                                                                 register.DocumentDb();
                                                                                 register.TeventStore(TeventStoreConnectionStringName);
                                                                                 configureContainer?.Invoke(register);
                                                                              }, composeEngine));
}
