using Compze.TypeIdentifiers;
using Compze.Abstractions.Public;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Internals.Transport.NamedPipes;

public static class NamedPipeInfrastructureQueryTransportRegistrar
{
   ///<summary>Registers the named-pipe implementation of the infrastructure-query transport that endpoint discovery runs on — the same-machine counterpart of <see cref="HttpInfrastructureQueryTransportRegistrar.HttpInfrastructureQueryTransport"/>.</summary>
   public static IComponentRegistrar NamedPipeInfrastructureQueryTransport(this IComponentRegistrar registrar)
      => registrar.Register(NamedPipeInfrastructureQueryTransportImplementation.RegisterWith);
}

class NamedPipeInfrastructureQueryTransportImplementation : IInfrastructureQueryTransport
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IInfrastructureQueryTransport>()
                                     .CreatedBy((IRemotableTessageSerializer serializer, ITypeMap typeMap) => new NamedPipeInfrastructureQueryTransportImplementation(serializer, typeMap)));

   readonly IRemotableTessageSerializer _serializer;
   readonly ITypeMap _typeMap;

   NamedPipeInfrastructureQueryTransportImplementation(IRemotableTessageSerializer serializer, ITypeMap typeMap)
   {
      _serializer = serializer;
      _typeMap = typeMap;
   }

   public async Task<TResult> GetAsync<TResult>(IQuery<TResult> query, EndpointAddress address)
   {
      var request = new NamedPipeTransportRequest(NamedPipeTransportRequestKind.InfrastructureQuery,
                                                  new TessageId(),
                                                  _typeMap.GetId(query.GetType()).CanonicalString,
                                                  _serializer.SerializeTessage(query));

      var responseJson = await NamedPipeTransportClient.SendAsync(request, address).caf();
      return _serializer.DeserializeResponse<TResult>(responseJson);
   }
}
