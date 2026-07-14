using Compze.Abstractions.Public;
using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Hosting.Public;
using Compze.Internals.Transport.NamedPipes;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Typermedia.Client;

public static class NamedPipeTypermediaTransportRegistrar
{
   ///<summary>Registers the named-pipe implementation of the Typermedia client transport — the same-machine counterpart of <see cref="HttpTypermediaTransportRegistrar.HttpTypermediaTransport"/>.</summary>
   public static IComponentRegistrar NamedPipeTypermediaTransport(this IComponentRegistrar registrar)
      => registrar.Register(Client.NamedPipeTypermediaTransport.RegisterWith);
}

///<summary>The named-pipe implementation of <see cref="ITypermediaTransport"/>: executes tueries and tommands against the<br/>
/// endpoint's named-pipe transport server (whose Typermedia request handling <see cref="NamedPipeTypermediaRequestHandlers"/><br/>
/// contributes) at the endpoint's typermedia address, with no web stack.</summary>
class NamedPipeTypermediaTransport : ITypermediaTransport
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITypermediaTransport>()
                                     .CreatedBy((IRemotableTessageSerializer serializer, ITypeMap typeMap) => new NamedPipeTypermediaTransport(serializer, typeMap)));

   readonly IRemotableTessageSerializer _serializer;
   readonly ITypeMap _typeMap;

   NamedPipeTypermediaTransport(IRemotableTessageSerializer serializer, ITypeMap typeMap)
   {
      _serializer = serializer;
      _typeMap = typeMap;
   }

   public async Task<TResult> GetAsync<TResult>(IRemotableTuery<TResult> tuery, EndpointAddress address)
      => _serializer.DeserializeResponse<TResult>(await SendAsync(NamedPipeTransportRequestKind.TypermediaTuery, tuery, address).caf());

   public async Task<TResult> PostAsync<TResult>(IAtMostOnceTommand<TResult> command, EndpointAddress address)
      => _serializer.DeserializeResponse<TResult>(await SendAsync(NamedPipeTransportRequestKind.TypermediaTommandWithResult, command, address).caf());

   public async Task PostAsync(IAtMostOnceTypermediaTommand command, EndpointAddress address)
      => await SendAsync(NamedPipeTransportRequestKind.TypermediaVoidTommand, command, address).caf();

   async Task<string> SendAsync(NamedPipeTransportRequestKind kind, IRemotableTessage tessage, EndpointAddress address)
   {
      var request = new NamedPipeTransportRequest(kind,
                                                  (tessage as IAtMostOnceTessage)?.Id ?? new TessageId(),
                                                  _typeMap.GetId(tessage.GetType()).CanonicalString,
                                                  _serializer.SerializeTessage(tessage));
      return await NamedPipeTransportClient.SendAsync(request, address).caf();
   }
}
