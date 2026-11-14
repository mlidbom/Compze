using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints;
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
      if(request.DeliveryStreamPosition is { } position)
      {
         content.Headers.Add(HttpConstants.Headers.SenderEndpointId, position.SenderEndpointId.Value.ToString());
         content.Headers.Add(HttpConstants.Headers.DeliveryStreamSequenceNumber, position.SequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture));
         content.Headers.Add(HttpConstants.Headers.DeliveryStreamPredecessorSequenceNumber, position.PredecessorSequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture));
      }

      //bug: A request that gets no response within HttpClient's timeout ends in a TaskCanceledException, which nobody asked
      //   for and which this does not catch. The timeout is .NET's default of 100 seconds, since HttpClientFactoryCE sets none,
      //   and it bounds two things that must be told apart: an endpoint whose machine vanished while it handled the request,
      //   and a handler that runs for longer. Calling the first an EndpointUnreachableException here would call the second one
      //   too. The framed-connection transports tell them apart: TCP keepalive (TcpConnection) ends a connection to a vanished
      //   machine within about ten seconds, and a handler is waited for as long as it runs. The todo: comment in
      //   HttpClientFactoryCE says what gives HTTP the same. A connect that times out is then what is left to catch here: it
      //   too is a TaskCanceledException whose InnerException is a TimeoutException.
      HttpResponseMessage response;
      try
      {
         response = await httpClient.PostAsync(requestUri, content, cancellationToken).caf();
      }
      catch(HttpRequestException noResponse) //The request got no response, where a response of any status is returned, not thrown: no connection to the address could be made, or it ended before the response arrived.
      {
         throw EndpointUnreachableException.BecauseTheRequestGotNoResponse("HTTP", address, request, noResponse);
      }

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
