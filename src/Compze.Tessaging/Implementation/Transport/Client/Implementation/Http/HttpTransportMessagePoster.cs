using Compze.Abstractions.Hosting.Public;
using Compze.Internals.Transport;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

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
                                     .CreatedBy((IHttpClientFactoryCE factory) => new HttpTransportMessagePoster(factory)));

   readonly IHttpClientFactoryCE _httpClientFactory;

   HttpTransportMessagePoster(IHttpClientFactoryCE httpClientFactory) => _httpClientFactory = httpClientFactory;

   static string GetRelativeUriForTessage(TransportTessage.OutGoing message)
   {
      switch(message.TessageTypeEnum)
      {
         case TransportTessageType.ExactlyOnceTevent:
            return HttpConstants.Routes.Tessaging.Tevent;
         case TransportTessageType.ExactlyOnceTommand:
            return HttpConstants.Routes.Tessaging.Tommand;
         default:
            throw new ArgumentOutOfRangeException();
      }
   }

   public async Task PostAsync(TransportTessage.OutGoing tessage, EndpointAddress endPointAddress) =>
      await PostAsyncInternal(tessage, new Uri(endPointAddress.Uri, GetRelativeUriForTessage(tessage))).caf();

   async Task<HttpResponseMessage> PostAsyncInternal(TransportTessage.OutGoing tessage, Uri requestUri)
   {
      using var httpClient = _httpClientFactory.CreateClient();

      using var content = new StringContent(tessage.Body);
      content.Headers.Add(HttpConstants.Headers.TessageId, tessage.TessageId.ToString());
      content.Headers.Add(HttpConstants.Headers.PayLoadTypeId, tessage.Type.CanonicalString);
      var response = await httpClient.PostAsync(requestUri, content).caf();
      if(!response.IsSuccessStatusCode)
      {
         var problemDetails = await ProblemDetails.FromResponse(response).caf();

         throw new MessageDispatchingFailedException($"""
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
