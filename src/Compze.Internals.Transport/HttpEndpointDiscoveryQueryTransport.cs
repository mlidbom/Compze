using Compze.TypeIdentifiers;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Internals.Transport;

public static class HttpEndpointDiscoveryQueryTransportRegistrar
{
   ///<summary>Registers the HTTP implementation of the endpoint-discovery query transport — the<br/>
   /// counterpart of <see cref="NamedPipes.NamedPipeEndpointDiscoveryQueryTransportRegistrar.NamedPipeEndpointDiscoveryQueryTransportIfNotRegistered"/> —<br/>
   /// and the <see cref="IHttpClientFactoryCE"/> it posts through. Guarded so that every HTTP transport registration demands it<br/>
   /// itself — a composing layer never registers it.</summary>
   public static IComponentRegistrar HttpEndpointDiscoveryQueryTransportIfNotRegistered(this IComponentRegistrar registrar)
      => registrar.IsRegistered<IEndpointDiscoveryQueryTransport>()
            ? registrar
            : registrar.HttpClientFactoryCEIfNotRegistered()
                       .Register(HttpEndpointDiscoveryQueryTransportImplementation.RegisterWith);
}

class HttpEndpointDiscoveryQueryTransportImplementation : IEndpointDiscoveryQueryTransport
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IEndpointDiscoveryQueryTransport>()
                                     .CreatedBy((IHttpClientFactoryCE factory, ITypeMap typeMap) => new HttpEndpointDiscoveryQueryTransportImplementation(factory, typeMap)));

   readonly IHttpClientFactoryCE _httpClientFactory;
   readonly ITypeMap _typeMap;

   HttpEndpointDiscoveryQueryTransportImplementation(IHttpClientFactoryCE httpClientFactory, ITypeMap typeMap)
   {
      _httpClientFactory = httpClientFactory;
      _typeMap = typeMap;
   }

   public async Task<TResult> GetAsync<TResult>(IQuery<TResult> query, EndpointAddress address)
   {
      var requestUri = new Uri(address.Uri, HttpConstants.Routes.EndpointDiscovery.Query);
      var body = EndpointDiscoverySerializer.SerializeQuery(query);
      var typeId = _typeMap.GetId(query.GetType());

      using var httpClient = _httpClientFactory.CreateClient();
      using var content = new StringContent(body);
      content.Headers.Add(HttpConstants.Headers.PayLoadTypeId, typeId.CanonicalString);

      var response = await httpClient.PostAsync(requestUri, content).caf();
      if(!response.IsSuccessStatusCode)
      {
         var problemDetails = await ProblemDetails.FromResponse(response).caf();

         throw new MessageDispatchingFailedException($"""
                                                      Uri:        {requestUri}
                                                      StatusCode: {response.StatusCode}
                                                      Type:       {query.GetType().FullName}
                                                      Body:
                                                      {body}

                                                      Exception Type: {problemDetails.Type}
                                                      Exception Tessage: {problemDetails.Detail}
                                                      """);
      }

      var resultJson = await response.Content.ReadAsStringAsync().caf();
      return EndpointDiscoverySerializer.DeserializeResult<TResult>(resultJson);
   }
}
