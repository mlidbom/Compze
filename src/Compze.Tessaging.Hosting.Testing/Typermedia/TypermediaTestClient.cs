using Compze.Tessaging.Endpoints.Discovery;
using Compze.Hosting.Configuration;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.Typermedia.Client;
using Compze.Tessaging.Hosting.Testing.Typermedia.Wiring;
using Compze.TypeIdentifiers;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;

namespace Compze.Tessaging.Hosting.Testing.Typermedia;

///<summary>
/// The pure client (<see cref="TypermediaClient"/>) composed for tests: it runs in its own container — wired with the
/// current test's pluggable components, never sharing the endpoint's — and connects to one endpoint's address
/// (<c>Endpoint.Address</c>, the endpoint's one transport-server address), exactly as an external client application would.
///</summary>
public class TypermediaTestClient : IAsyncDisposable
{
   readonly TypermediaClient _client;

   public IRemoteTypermediaNavigator Navigator => _client.Navigator;

   TypermediaTestClient(TypermediaClient client) => _client = client;

   public static async Task<TypermediaTestClient> ConnectTo(EndpointAddress endpointAddress, Action<ITypeMapper> registerDomainTypeMappings)
   {
      var client = TypermediaClient.Compose(TestEnv.DIContainer.CreateTestingContainerBuilder(), compose =>
      {
         compose.TransportProtocol(registrar => registrar.CurrentTestsEndpointTransportClient());
         compose.Serializer(registrar => registrar.CurrentTestsSerializersIfNotClonedContainer());
         //Tests register their domain mappings explicitly, exactly as a production client does, so a test that forgets to
         //register a type fails the same way the real application would - there is no AppDomain-wide scan.
         compose.MapTypes(registerDomainTypeMappings);
         compose.Registrar.JSonAppConfigFileConfigurationParameterProvider();
      });

      var testClient = new TypermediaTestClient(client);
      await client.ConnectAsync(endpointAddress).caf();
      return testClient;
   }

   ///<summary>Connects this client to one more endpoint's address — an external client navigating several known endpoints.</summary>
   public async Task AlsoConnectTo(EndpointAddress endpointAddress) => await _client.ConnectAsync(endpointAddress).caf();

   public async ValueTask DisposeAsync() => await _client.DisposeAsync().caf();
}
