using System;
using System.Net.Http;
using System.Threading.Tasks;
using Compze.Core.Serialization.Internal;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;

static class HttpApiTransportClientRegistrar
{
   internal static IComponentRegistrar HttpApiTransportClient(this IComponentRegistrar registrar)
      => registrar.Register(Http.HttpTransportMessagePoster.RegisterWith);
}

class HttpTransportMessagePoster : ITransportMessagePoster
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITransportMessagePoster>()
                                     .CreatedBy((IHttpClientFactoryCE factory, IRemotableTessageSerializer serializer) => new HttpTransportMessagePoster(factory, serializer)));

   readonly IHttpClientFactoryCE _httpClientFactory;
   readonly IRemotableTessageSerializer _serializer;

   HttpTransportMessagePoster(IHttpClientFactoryCE httpClientFactory, IRemotableTessageSerializer serializer)
   {
      _httpClientFactory = httpClientFactory;
      _serializer = serializer;
   }

   public async Task<TResult> PostAsync<TResult>(TransportTessage.OutGoing tessage, object realTessage, Uri requestUri)
   {
      var response = await PostAsyncInternal(tessage, realTessage, requestUri).caf();

      var resultJson = await response.Content.ReadAsStringAsync().caf();
      var result = _serializer.DeserializeResponse<TResult>(resultJson);
      return result;
   }

   public async Task PostAsync(TransportTessage.OutGoing tessage, object realTessage, Uri requestUri) =>
      await PostAsyncInternal(tessage, realTessage, requestUri).caf();

   async Task<HttpResponseMessage> PostAsyncInternal(TransportTessage.OutGoing tessage, object realTessage, Uri requestUri)
   {
      using var httpClient = _httpClientFactory.CreateClient();

      var content = new StringContent(tessage.Body);
      content.Headers.Add(HttpConstants.Headers.TessageId, tessage.Id.ToString());
      content.Headers.Add(HttpConstants.Headers.PayLoadTypeId, tessage.Type.GuidValue.ToString());
      var response = await httpClient.PostAsync(requestUri, content).caf();
      if(!response.IsSuccessStatusCode)
      {
         var problemDetails = await ProblemDetails.FromResponse(response).caf();

         throw new TessageDispatchingFailedException($"""
                                                      Uri:        {requestUri}
                                                      StatusCode: {response.StatusCode}
                                                      Type:       {realTessage.GetType().FullName}
                                                      Body:
                                                      {tessage.Body}

                                                      Exception Type: {problemDetails.Type}
                                                      Exception Tessage: {problemDetails.Detail}
                                                      """);
      }

      return response;
   }
}
