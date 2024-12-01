using System;
using System.Net.Http;
using System.Threading.Tasks;
using Compze.Messaging.Buses.Http;
using Compze.Serialization;
using Compze.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Messaging.Buses.Implementation.Http;

class HttpApiClient(IHttpClientFactoryCE clientFactory, IRemotableMessageSerializer serializer) : IHttpApiClient
{
   readonly IHttpClientFactoryCE _clientFactory = clientFactory;
   readonly IRemotableMessageSerializer _serializer = serializer;

   public async Task<TResult> PostAsync<TResult>(TransportMessage.OutGoing message, object realMessage, Uri requestUri)
   {
      var response = await PostAsync(message, realMessage, requestUri).CaF();

      var resultJson = await response.Content.ReadAsStringAsync().CaF();
      var result = (TResult)_serializer.DeserializeResponse(typeof(TResult), resultJson);
      return result;
   }

   public async Task<HttpResponseMessage> PostAsync(TransportMessage.OutGoing message, object realMessage, Uri requestUri)
   {
      using var it = _clientFactory.CreateClient();

      var content = new StringContent(message.Body);
      content.Headers.Add(HttpConstants.Headers.MessageId, message.Id.ToString());
      content.Headers.Add(HttpConstants.Headers.PayLoadTypeId, message.Type.GuidValue.ToString());
      var response = await it.PostAsync(requestUri, content).CaF();
      if(!response.IsSuccessStatusCode)
      {
         var problemDetails = await ProblemDetails.FromResponse(response).CaF();

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
