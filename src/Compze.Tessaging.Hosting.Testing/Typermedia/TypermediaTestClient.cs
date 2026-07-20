using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Configuration;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.Hosting.Testing.Typermedia.Wiring;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.Typermedia.Client;

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

   //todo: This just taking an IComponentRegistrar feels iffy. Can we make setting up the type mappings easier and safer to do?
   //todo: With registries implemented, should this just have a single hardcoded address to connect to?
   /// <param name="endpointAddress">The endpoint to connect to</param>
   /// <param name="declareRequiredTypeMappings">Used to configure the domain assemblies whose type identity this client needs</param>
   public static async Task<TypermediaTestClient> ConnectTo(EndpointAddress endpointAddress, Action<IComponentRegistrar> declareRequiredTypeMappings)
   {
      var client = TypermediaClient.Build(TestEnv.DIContainer.CreateTestingContainerBuilder(),
                                          builder =>
                                          {
                                             builder.ConfigureTransport(registrar => registrar.CurrentTestsEndpointTransportClient())
                                                    .ConfigureSerializer(registrar => registrar.CurrentTestsSerializersIfNotClonedContainer())
                                                    .DeclareRequiredTypeMappings(declareRequiredTypeMappings);
                                             builder.Registrar.JSonAppConfigFileConfigurationParameterProvider();
                                          });

      var testClient = new TypermediaTestClient(client);
      await client.ConnectAsync(endpointAddress).caf();
      return testClient;
   }

   ///<summary>Connects this client to one more endpoint's address — an external client navigating several known endpoints.</summary>
   public async Task AlsoConnectTo(EndpointAddress endpointAddress) => await _client.ConnectAsync(endpointAddress).caf();

   public async ValueTask DisposeAsync() => await _client.DisposeAsync().caf();
}
