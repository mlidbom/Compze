using System;
using System.Net.Http;
using System.Threading.Tasks;
using Compze.Serialization;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Hosting.Implementation.Http;

static class HttpApiClientRegistrar
{
   internal static IComponentRegistrar HttpApiClient(this IComponentRegistrar registrar)
      => registrar.Register(Http.HttpApiTransportClient.RegisterWith);
}

class HttpApiTransportClient : IRemoteApiTransportClient
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IRemoteApiTransportClient>()
                                     .CreatedBy((IHttpClientFactoryCE factory, IRemotableMessageSerializer serializer) => new HttpApiTransportClient(factory, serializer)));

   readonly IHttpClientFactoryCE _clientFactory;
   readonly IRemotableMessageSerializer _serializer;

   HttpApiTransportClient(IHttpClientFactoryCE clientFactory, IRemotableMessageSerializer serializer)
   {
      _clientFactory = clientFactory;
      _serializer = serializer;
   }

   public async Task<TResult> PostAsync<TResult>(TransportMessage.OutGoing message, object realMessage, Uri requestUri)
   {
      var response = await PostAsync(message, realMessage, requestUri).caf();

      var resultJson = await response.Content.ReadAsStringAsync().caf();
      var result = (TResult)_serializer.DeserializeResponse(typeof(TResult), resultJson);
      return result;
   }

   public async Task<HttpResponseMessage> PostAsync(TransportMessage.OutGoing message, object realMessage, Uri requestUri)
   {
      using var it = _clientFactory.CreateClient();

      var content = new StringContent(message.Body);
      content.Headers.Add(HttpConstants.Headers.MessageId, message.Id.ToString());
      content.Headers.Add(HttpConstants.Headers.PayLoadTypeId, message.Type.GuidValue.ToString());
      var response = await it.PostAsync(requestUri, content).caf();
      if(!response.IsSuccessStatusCode)
      {
         var problemDetails = await ProblemDetails.FromResponse(response).caf();

         throw new MessageDispatchingFailedException($"""
                                                      Uri:        {requestUri}
                                                      StatusCode: {response.StatusCode}
                                                      Type:       {realMessage.GetType().FullName}
                                                      Body:
                                                      {message.Body}

                                                      Exception Type: {problemDetails.Type}
                                                      Exception Message: {problemDetails.Detail}
                                                      """);
      }

      return response;
   }
}
