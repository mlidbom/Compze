using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging._internal.Transport;
using Compze.Tessaging.Typermedia;

namespace Compze.Tessaging._private.Transport.AspNetCore;

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

      throw new TessageDispatchingFailedException($"""
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
         TransportRequestKind.EndpointDiscoveryQuery => HttpConstants.Routes.EndpointInformation.Query,
         _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, message: null)
      };
}
