using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Serialization.Internal;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Internals.Transport;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging.Typermedia.Client;

///<summary>
/// The declaration surface a <see cref="TypermediaClient"/> is composed through — what <see cref="TypermediaClient.Compose"/>
/// hands its composition callback. The pure client declares its transport-client protocol (<see cref="TransportProtocol"/>),
/// its serializer (<see cref="Serializer"/>), and its own type mappings (<see cref="MapTypes"/>) — the mirror of what the
/// endpoints it navigates map, declared explicitly so a client that forgets a type fails the same way the real application
/// would. The builder exists only inside the composition callback; declaring after the build explodes.
///</summary>
public sealed class TypermediaClientBuilder
{
   readonly IContainerBuilder _containerBuilder;
   readonly TypeMapper _typeMapper = new();
   Action<IComponentRegistrar>? _registerTransportProtocol;
   Action<IComponentRegistrar>? _registerSerializer;
   bool _composed;

   internal TypermediaClientBuilder(IContainerBuilder containerBuilder) => _containerBuilder = containerBuilder;

   ///<summary>Registers the client's components with its container.</summary>
   public IComponentRegistrar Registrar => _containerBuilder.Registrar;

   ///<summary>Declares the client's type-id mappings — the mirror of what the endpoints it navigates map.</summary>
   public void MapTypes(Action<ITypeMapper> map)
   {
      AssertStillComposing();
      map(_typeMapper);
   }

   ///<summary>Declares the client's transport protocol — the transport-client strategy only: a pure client runs no server.</summary>
   public void TransportProtocol(Action<IComponentRegistrar> registerProtocolClient)
   {
      AssertStillComposing();
      State.Assert(_registerTransportProtocol is null, () => "The client already declared its transport protocol — a client speaks exactly one.");
      _registerTransportProtocol = registerProtocolClient;
   }

   ///<summary>Declares the client's serializer — see <c>EndpointBuilder.Serializer</c>, whose declaration idiom this is: a<br/>
   /// composition whose container already carries the serializers (a testing container) declares none.</summary>
   public void Serializer(Action<IComponentRegistrar> registerSerializer)
   {
      AssertStillComposing();
      State.Assert(_registerSerializer is null, () => "The client already declared its serializer — a client has exactly one.");
      _registerSerializer = registerSerializer;
   }

   void AssertStillComposing() =>
      State.Assert(!_composed, () => "The client is already composed — the declaration surface exists only inside the composition callback.");

   internal TypermediaClient Build()
   {
      State.Assert(_registerTransportProtocol is not null,
                   () => "The client declares no transport protocol. Declare the transport-client strategy in the composition — e.g. client.TransportProtocol(registrar => registrar.NamedPipeEndpointTransportClientIfNotRegistered()).");
      State.Assert(_registerSerializer is not null || Registrar.IsRegistered<ITypermediaSerializer>(),
                   () => "The client declares no serializer. Declare it in the composition — e.g. client.NewtonsoftSerializer(). (A testing container already carrying the suite's serializers declares none.)");

      _typeMapper.MapTypesFromAssemblyContaining<EndpointAddress>();         // Compze.Abstractions — the shared message-type hierarchy
      _typeMapper.MapTypesFromAssemblyContaining<EndpointInformation>();     // Compze.Tessaging — the endpoint-discovery types the client's router reads advertisements through

      Registrar.Register(Singleton.For<ITypeMapper>().Instance(_typeMapper),
                         Singleton.For<ITypeMap>().Instance(_typeMapper));

      _registerTransportProtocol!(Registrar);
      _registerSerializer?.Invoke(Registrar);

      Registrar.TypermediaTransport()
               .TypermediaClientRouter()
               .RemoteTypermediaNavigator();

      _composed = true;
      return new TypermediaClient(_containerBuilder.Build());
   }
}
