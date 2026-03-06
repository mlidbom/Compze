using Compze.Abstractions.Serialization.Internal;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;

public static class HttpApiTransportClientRegistrar
{
   public static IComponentRegistrar HttpApiTransportClient(this IComponentRegistrar registrar)
      => registrar.Register(HttpTransportMessagePoster.RegisterWith);
}

class HttpTransportMessagePoster : ITransportMessagePoster
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITransportMessagePoster>()
                                     .CreatedBy((IHttpClientFactoryCE factory, IRemotableTessageSerializer serializer) => new HttpTransportMessagePoster(factory, serializer)));

   readonly IHttpClientFactoryCE _httpClientFactory;
   readonly IRemotableTessageSerializer _serializer;

   HttpTransportMessagePoster(IHttpClientFactoryCE httpClientFactory, IRemotableTessageSerializer serializer)
   {
      _httpClientFactory = httpClientFactory;
      _serializer = serializer;
   }

   public async Task<TResult> PostAsync<TResult>(TransportTessage.OutGoing tessage, EndPointAddress endPointAddress)
   {
      var response = await PostAsyncInternal(tessage, new Uri(endPointAddress.Uri, GetRelativeUriForTessage(tessage))).caf();

      var resultJson = await response.Content.ReadAsStringAsync().caf();
      var result = _serializer.DeserializeResponse<TResult>(resultJson);
      return result;
   }

   static string GetRelativeUriForTessage(TransportTessage.OutGoing message)
   {
      switch(message.TessageTypeEnum)
      {
         case TransportTessageType.ExactlyOnceTevent:
            return HttpConstants.Routes.Tessaging.Tevent;
         case TransportTessageType.TypermediaAtMostOnceTommand:
            return HttpConstants.Routes.Typermedia.TommandNoResult;
         case TransportTessageType.TypermediaAtMostOnceTommandWithReturnValue:
            return HttpConstants.Routes.Typermedia.TommandWithResult;
         case TransportTessageType.ExactlyOnceTommand:
            return HttpConstants.Routes.Tessaging.Tommand;
         case TransportTessageType.TyperMediaTuery:
            return HttpConstants.Routes.Typermedia.Tuery;
         default:
            throw new ArgumentOutOfRangeException();
      }
   }

   public async Task PostAsync(TransportTessage.OutGoing tessage, EndPointAddress endPointAddress) =>
      await PostAsyncInternal(tessage, new Uri(endPointAddress.Uri, GetRelativeUriForTessage(tessage))).caf();

   async Task<HttpResponseMessage> PostAsyncInternal(TransportTessage.OutGoing tessage, Uri requestUri)
   {
      using var httpClient = _httpClientFactory.CreateClient();

      using var content = new StringContent(tessage.Body);
      content.Headers.Add(HttpConstants.Headers.TessageId, tessage.TessageId.ToString());
      content.Headers.Add(HttpConstants.Headers.PayLoadTypeId, tessage.Type.ToString());
      var response = await httpClient.PostAsync(requestUri, content).caf();
      if(!response.IsSuccessStatusCode)
      {
         var problemDetails = await ProblemDetails.FromResponse(response).caf();

         throw new TessageDispatchingFailedException($"""
                                                      Uri:        {requestUri}
                                                      StatusCode: {response.StatusCode}
                                                      Type:       {tessage.Tessage.GetType().FullName}
                                                      Body:
                                                      {tessage.Body}

                                                      Exception Type: {problemDetails.Type}
                                                      Exception Tessage: {problemDetails.Detail}
                                                      """);
      }

      return response;
   }
}
