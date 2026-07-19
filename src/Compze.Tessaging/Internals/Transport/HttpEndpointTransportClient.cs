using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Wiring.Registration;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Internals.Transport;

public static class HttpEndpointTransportClientRegistrar
{
   ///<summary>Registers the HTTP implementation of the endpoint transport's client side (<see cref="IEndpointTransportClient"/>),<br/>
   /// plus the <see cref="IHttpClientFactoryCE"/> it posts through. Guarded so that every HTTP transport registration can demand it<br/>
   /// without conflicting when an endpoint speaks several communication styles.</summary>
   public static IComponentRegistrar HttpEndpointTransportClientIfNotRegistered(this IComponentRegistrar registrar)
      => registrar.IsRegistered<IEndpointTransportClient>()
            ? registrar
            : registrar.HttpClientFactoryCEIfNotRegistered()
                       .Register(Singleton.For<IEndpointTransportClient>()
                                          .CreatedBy((IHttpClientFactoryCE factory) => new HttpEndpointTransportClient(factory)));
}

///<summary>The HTTP implementation of <see cref="IEndpointTransportClient"/>: posts each request to its kind's route<br/>
/// (<see cref="HttpConstants.Routes"/>) on the endpoint's ASP.NET Core transport server, carrying the envelope identity in the<br/>
/// <see cref="HttpConstants.Headers"/> and the serialized tessage as the request body.</summary>
class HttpEndpointTransportClient : IEndpointTransportClient
{
   readonly IHttpClientFactoryCE _httpClientFactory;

   internal HttpEndpointTransportClient(IHttpClientFactoryCE httpClientFactory) => _httpClientFactory = httpClientFactory;

   public async Task<string> SendAsync(TransportRequest request, EndpointAddress address, CancellationToken cancellationToken = default)
   {
      var requestUri = new Uri(address.Uri, RouteFor(request.Kind));

      using var httpClient = _httpClientFactory.CreateClient();
      using var content = new StringContent(request.Body);
      content.Headers.Add(HttpConstants.Headers.TessageId, request.TessageId.ToString());
      content.Headers.Add(HttpConstants.Headers.PayLoadTypeId, request.PayloadTypeIdString);

      var response = await httpClient.PostAsync(requestUri, content, cancellationToken).caf();
      if(response.IsSuccessStatusCode) return await response.Content.ReadAsStringAsync(cancellationToken).caf();

      var problemDetails = await ProblemDetails.FromResponse(response).caf();

      throw new MessageDispatchingFailedException($"""
                                                   Uri:        {requestUri}
                                                   StatusCode: {response.StatusCode}
                                                   Kind:       {request.Kind}
                                                   Type:       {request.PayloadTypeIdString}
                                                   Body:
                                                   {request.Body}

                                                   Exception Type: {problemDetails.Type}
                                                   Exception Tessage: {problemDetails.Detail}
                                                   """);
   }

   static string RouteFor(TransportRequestKind kind) =>
      kind switch
      {
         TransportRequestKind.ExactlyOnceTevent => HttpConstants.Routes.Tessaging.Tevent,
         TransportRequestKind.ExactlyOnceTommand => HttpConstants.Routes.Tessaging.Tommand,
         TransportRequestKind.BestEffortTevent => HttpConstants.Routes.Tessaging.BestEffortTevent,
         TransportRequestKind.TypermediaTuery => HttpConstants.Routes.Typermedia.Tuery,
         TransportRequestKind.TypermediaTommandWithResult => HttpConstants.Routes.Typermedia.TommandWithResult,
         TransportRequestKind.TypermediaVoidTommand => HttpConstants.Routes.Typermedia.TommandNoResult,
         TransportRequestKind.EndpointDiscoveryQuery => HttpConstants.Routes.EndpointDiscovery.Query,
         _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, message: null)
      };
}
