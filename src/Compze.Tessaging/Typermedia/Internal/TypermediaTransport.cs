using Compze.Tessaging.Typermedia.Internal;
using Compze.TypeIdentifiers;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Internal.Transport;
using Compze.Tessaging.Internal.Transport.Advertisement;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Typermedia.Internal;

static class TypermediaTransportRegistrar
{
   ///<summary>Registers the client side of the Typermedia transport (<see cref="TypermediaTransport"/>) plus the<br/>
   /// endpoint-discovery query transport it discovers endpoints through — both run on the endpoint transport client<br/>
   /// (<see cref="IEndpointTransportClient"/>) that the endpoint's protocol declaration (or a client-only composition's<br/>
   /// transport-client registration) supplies.</summary>
   internal static IComponentRegistrar TypermediaTransport(this IComponentRegistrar registrar)
      => registrar.EndpointInformationQueryTransportIfNotRegistered()
                  .Register(Internal.TypermediaTransport.RegisterWith);
}

///<summary>The client side of the Typermedia transport: executes tueries and tommands against a remote endpoint through the<br/>
/// endpoint transport client (<see cref="IEndpointTransportClient"/>) — one implementation for every protocol, since the<br/>
/// protocol difference lives entirely in the transport client.</summary>
class TypermediaTransport : ITypermediaTransport
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITypermediaTransport>()
                                     .CreatedBy((IEndpointTransportClient transportClient, ITypermediaSerializer serializer, ITypeMap typeMap) => new TypermediaTransport(transportClient, serializer, typeMap)));

   readonly IEndpointTransportClient _transportClient;
   readonly ITypermediaSerializer _serializer;
   readonly ITypeMap _typeMap;

   TypermediaTransport(IEndpointTransportClient transportClient, ITypermediaSerializer serializer, ITypeMap typeMap)
   {
      _transportClient = transportClient;
      _serializer = serializer;
      _typeMap = typeMap;
   }

   public async Task<TResult> GetAsync<TResult>(IRemotableTuery<TResult> tuery, EndpointAddress address)
      => _serializer.DeserializeResult<TResult>(await SendAsync(TransportRequestKind.TypermediaTuery, tuery, address).caf());

   public async Task<TResult> PostAsync<TResult>(IAtMostOnceTypermediaTommand<TResult> command, EndpointAddress address)
      => _serializer.DeserializeResult<TResult>(await SendAsync(TransportRequestKind.TypermediaTommandWithResult, command, address).caf());

   public async Task PostAsync(IAtMostOnceTypermediaTommand command, EndpointAddress address)
      => await SendAsync(TransportRequestKind.TypermediaVoidTommand, command, address).caf();

   async Task<string> SendAsync(TransportRequestKind kind, ITypermediaTessage tessage, EndpointAddress address)
   {
      var request = new TransportRequest(kind,
                                         (tessage as IAtMostOnceTessage)?.Id ?? new TessageId(),
                                         _typeMap.GetId(tessage.GetType()).CanonicalString,
                                         _serializer.SerializeTessage(tessage));
      return await _transportClient.SendAsync(request, address).caf();
   }
}
