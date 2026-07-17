using Compze.Abstractions.Hosting.Public;
using Compze.Hosting.Configuration;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.Typermedia.Client;
using Compze.Tessaging.Hosting.Testing.Typermedia.Wiring;
using Compze.TypeIdentifiers;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;

namespace Compze.Tessaging.Hosting.Testing.Typermedia;

///<summary>
/// A remote Typermedia client for tests: it runs in its own container — wired with the current test's pluggable
/// components, never sharing the endpoint's — and connects over HTTP to one endpoint's typermedia address
/// (see <see cref="EndpointTypermediaExtensions"/>), exactly as an external client application would.
///</summary>
public class TypermediaTestClient : IAsyncDisposable
{
   readonly IDependencyInjectionContainer _container;
   readonly ITypermediaRouter _typermediaRouter;

   public IRemoteTypermediaNavigator Navigator { get; }

   TypermediaTestClient(IDependencyInjectionContainer container)
   {
      _container = container;
      _typermediaRouter = container.Resolve<ITypermediaRouter>();
      Navigator = container.Resolve<IRemoteTypermediaNavigator>();
   }

   public static async Task<TypermediaTestClient> ConnectTo(EndpointAddress typermediaAddress, Action<ITypeMapper> registerDomainTypeMappings)
   {
#pragma warning disable CA2000 // We are passing this disposable into a constructor of an object we don't own
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
#pragma warning restore CA2000
      builder.Registrar
             .CurrentTestsSerializersIfNotClonedContainer()
             .CurrentTestsTypermediaClientTransport()
             .JSonAppConfigFileConfigurationParameterProvider()
             .TypermediaClientTypeIdentifierMapper(registerDomainTypeMappings)
             .TypermediaRouter()
             .RemoteTypermediaNavigator();

      var client = new TypermediaTestClient(builder.Build());
      client._typermediaRouter.Start();
      await client._typermediaRouter.ConnectAsync(typermediaAddress).caf();
      return client;
   }

   ///<summary>Connects this client to one more endpoint's typermedia address — an external client navigating several known endpoints.</summary>
   public async Task AlsoConnectTo(EndpointAddress typermediaAddress) => await _typermediaRouter.ConnectAsync(typermediaAddress).caf();

   public async ValueTask DisposeAsync()
   {
      _typermediaRouter.Stop();
      await _container.DisposeAsync().caf();
   }
}

public static class TypermediaClientTypeIdentifierMapperRegistrar
{
   ///<summary>
   /// Registers a <see cref="TypeMapper"/> populated with the framework mappings a Typermedia client needs — the
   /// mirror of what a Typermedia endpoint's server side maps — plus the caller's domain mappings. Tests register
   /// their domain explicitly, exactly as a production client does, so a test that forgets to register a type
   /// fails the same way the real application would, and there is no AppDomain-wide scan.
   ///</summary>
   public static IComponentRegistrar TypermediaClientTypeIdentifierMapper(this IComponentRegistrar @this, Action<ITypeMapper> registerDomainTypeMappings)
   {
      var mapper = new TypeMapper();
      mapper.MapTypesFromAssemblyContaining<EndpointAddress>();               // Compze.Abstractions — the shared message-type hierarchy
      mapper.MapTypesFromAssemblyContaining<TypermediaEndpointInformation>(); // Compze.Tessaging.Typermedia.Client — the typermedia discovery types
      registerDomainTypeMappings(mapper);
      return @this.Register(Singleton.For<ITypeMapper>().Instance(mapper))
                  .Register(Singleton.For<ITypeMap>().Instance(mapper));
   }
}
