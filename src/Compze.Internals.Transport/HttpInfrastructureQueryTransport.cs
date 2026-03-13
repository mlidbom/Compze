using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Internals.Transport;

public static class HttpInfrastructureQueryTransportRegistrar
{
   public static IComponentRegistrar HttpInfrastructureQueryTransport(this IComponentRegistrar registrar)
      => registrar.Register(HttpInfrastructureQueryTransportImplementation.RegisterWith);
}

class HttpInfrastructureQueryTransportImplementation : IInfrastructureQueryTransport
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IInfrastructureQueryTransport>()
                                     .CreatedBy((IHttpClientFactoryCE factory, IRemotableTessageSerializer serializer, ITypeMapper typeMapper) => new HttpInfrastructureQueryTransportImplementation(factory, serializer, typeMapper)));

   readonly IHttpClientFactoryCE _httpClientFactory;
   readonly IRemotableTessageSerializer _serializer;
   readonly ITypeMapper _typeMapper;

   HttpInfrastructureQueryTransportImplementation(IHttpClientFactoryCE httpClientFactory, IRemotableTessageSerializer serializer, ITypeMapper typeMapper)
   {
      _httpClientFactory = httpClientFactory;
      _serializer = serializer;
      _typeMapper = typeMapper;
   }

   public async Task<TResult> GetAsync<TResult>(IQuery<TResult> query, EndPointAddress address)
   {
      var requestUri = new Uri(address.Uri, HttpConstants.Routes.Infrastructure.Query);
      var body = _serializer.SerializeTessage(query);
      var typeId = _typeMapper.GetId(query.GetType());

      using var httpClient = _httpClientFactory.CreateClient();
      using var content = new StringContent(body);
      content.Headers.Add(HttpConstants.Headers.PayLoadTypeId, typeId.ToString());

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
      return _serializer.DeserializeResponse<TResult>(resultJson);
   }
}
