using Compze.Tessaging.Typermedia.Client._private;
using Compze.Tessaging.Typermedia._internal;
using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Typermedia._private;

namespace Compze.Tessaging.Typermedia.Client;

///<summary>Builds a <see cref="TypermediaClient"/>. What <see cref="TypermediaClient.Build"/> hands its build callback.</summary>
public sealed class TypermediaClientBuilder
{
   readonly IContainerBuilder _containerBuilder;
   Action<IComponentRegistrar>? _registerTransportProtocol;
   Action<IComponentRegistrar>? _configureSerializer;
   Action<IComponentRegistrar>? _declareRequiredTypeMappings;
   bool _composed;

   internal TypermediaClientBuilder(IContainerBuilder containerBuilder) => _containerBuilder = containerBuilder;

   ///<summary>Registers the client's components with its container.</summary>
   internal IComponentRegistrar Registrar => _containerBuilder.Registrar;

   //todo: This just taking an IComponentRegistrar feels iffy. Can we make setting up the transport easier and safer to do?
   ///<summary>Declares the client's transport protocol — the transport-client strategy only: a pure client runs no server.</summary>
   public TypermediaClientBuilder ConfigureTransport(Action<IComponentRegistrar> configure)
   {
      AssertStillComposing();
      State.Assert(_registerTransportProtocol is null, () => "The client already declared its transport protocol — a client speaks exactly one.");
      _registerTransportProtocol = configure;
      return this;
   }

   //todo: This just taking an IComponentRegistrar feels iffy. Can we make setting up the serializer easier and safer to do?
   ///<summary>Configures the serializer the client will use.</summary>
   public TypermediaClientBuilder ConfigureSerializer(Action<IComponentRegistrar> configure)
   {
      AssertStillComposing();
      State.Assert(_configureSerializer is null, () => "The client already declared its serializer — a client has exactly one.");
      _configureSerializer = configure;
      return this;
   }

   ///<summary>Declares the type mappings the client requires — the assemblies whose type identity its conversations use<br/>
   /// (<c>registrar.RequireMappedTypesFromAssemblyContaining&lt;T&gt;()</c>).</summary>
   public TypermediaClientBuilder DeclareRequiredTypeMappings(Action<IComponentRegistrar> declare)
   {
      AssertStillComposing();
      State.Assert(_declareRequiredTypeMappings is null, () => "The client already declared its required type mappings.");
      _declareRequiredTypeMappings = declare;
      return this;
   }

   void AssertStillComposing() =>
      State.Assert(!_composed, () => "The client is already composed — the declaration surface exists only inside the composition callback.");

   internal TypermediaClient Build()
   {
      State.Assert(_registerTransportProtocol is not null,
                   () => "The client declares no transport protocol. Declare the transport-client strategy in the composition — e.g. client.ConfigureTransport(registrar => registrar.NamedPipeEndpointTransportClientIfNotRegistered()).");
      State.Assert(_configureSerializer is not null || Registrar.IsRegistered<ITypermediaSerializer>(),
                   () => "The client declares no serializer. Declare it in the composition — e.g. client.NewtonsoftSerializer(). (A testing container already carrying the suite's serializers declares none.)");

      _registerTransportProtocol!(Registrar);
      _configureSerializer?.Invoke(Registrar);
      _declareRequiredTypeMappings?.Invoke(Registrar);

      Registrar.TypermediaTransport()
               .TypermediaClientRouter()
               .RemoteTypermediaNavigator();

      _composed = true;
      return new TypermediaClient(_containerBuilder.Build());
   }
}
