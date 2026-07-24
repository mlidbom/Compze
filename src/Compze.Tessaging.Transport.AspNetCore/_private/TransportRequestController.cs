using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging._internal.Transport;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Compze.Tessaging._private.Transport.AspNetCore;

using Compze.Tessaging._private.Transport.Advertisement;

namespace Compze.Tessaging.Transport.AspNetCore._private;

#pragma warning disable CA1031 //We catch all exceptions here to route them back to the client, exactly as the named-pipe transport server does.

///<summary>The ASP.NET Core dispatch surface of the endpoint's transport server: receives every communication style's requests<br/>
/// on their per-kind routes (<see cref="HttpConstants.Routes"/>), rebuilds each <see cref="TransportRequest"/> from route, headers<br/>
/// and body, and dispatches it through the endpoint's one <see cref="TransportRequestHandlerMap"/>. A handler exception travels<br/>
/// back as a problem response the client rethrows as <see cref="TessageDispatchingFailedException"/> — the web-stack counterpart<br/>
/// of the named-pipe transport server's request serving.</summary>
class TransportRequestController : Controller
{
   internal static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(Scoped.For<TransportRequestController>()
                               .CreatedBy((TransportRequestHandlerMap handlers) => new TransportRequestController(handlers)));

   readonly TransportRequestHandlerMap _handlers;

   TransportRequestController(TransportRequestHandlerMap handlers) => _handlers = handlers;

   [HttpPost(HttpConstants.Routes.Tessaging.Tevent)]
   public Task<IActionResult> ExactlyOnceTevent() => DispatchAsync(TransportRequestKind.ExactlyOnceTevent);

   [HttpPost(HttpConstants.Routes.Tessaging.Tommand)]
   public Task<IActionResult> ExactlyOnceTommand() => DispatchAsync(TransportRequestKind.ExactlyOnceTommand);

   [HttpPost(HttpConstants.Routes.Tessaging.BestEffortTevent)]
   public Task<IActionResult> BestEffortTevent() => DispatchAsync(TransportRequestKind.BestEffortTevent);

   [HttpPost(HttpConstants.Routes.Typermedia.Tuery)]
   public Task<IActionResult> TypermediaTuery() => DispatchAsync(TransportRequestKind.TypermediaTuery);

   [HttpPost(HttpConstants.Routes.Typermedia.TommandWithResult)]
   public Task<IActionResult> TypermediaTommandWithResult() => DispatchAsync(TransportRequestKind.TypermediaTommandWithResult);

   [HttpPost(HttpConstants.Routes.Typermedia.TommandNoResult)]
   public Task<IActionResult> TypermediaVoidTommand() => DispatchAsync(TransportRequestKind.TypermediaVoidTommand);

   [HttpPost(HttpConstants.Routes.EndpointInformation.Query)]
   public Task<IActionResult> EndpointDiscoveryQuery() => DispatchAsync(TransportRequestKind.EndpointDiscoveryQuery);

   async Task<IActionResult> DispatchAsync(TransportRequestKind kind)
   {
      var request = await ReadRequestAsync(kind).caf();

      try
      {
         var responsePayload = await RunOutsideRequestContext(() => _handlers.HandleAsync(request)).caf();
         return Ok(responsePayload);
      }
      catch(Exception exception)
      {
         this.Log().Warning(exception, $"Exception handling {kind}");
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }

   async Task<TransportRequest> ReadRequestAsync(TransportRequestKind kind)
   {
      var tessageId = new TessageId(Guid.Parse(Request.Headers[HttpConstants.Headers.TessageId][0]._assert().NotNull()));
      var payloadTypeIdString = Request.Headers[HttpConstants.Headers.PayLoadTypeId][0]._assert().NotNull();

      using var reader = new StreamReader(HttpContext.Request.Body);
      var body = await reader.ReadToEndAsync().caf();

      return new TransportRequest(kind, tessageId, payloadTypeIdString, body, ReadDeliveryStreamPosition(kind));
   }

   ///<summary>The exactly-once kinds always carry the tessage's <see cref="DeliveryStreamPosition"/> in the<br/>
   /// <see cref="HttpConstants.Headers"/>, so its presence is decided by the request's kind, mirroring the client's writing.</summary>
   DeliveryStreamPosition? ReadDeliveryStreamPosition(TransportRequestKind kind)
   {
      if(kind is not (TransportRequestKind.ExactlyOnceTevent or TransportRequestKind.ExactlyOnceTommand)) return null;
      var senderEndpointId = new EndpointId(Guid.Parse(Request.Headers[HttpConstants.Headers.SenderEndpointId][0]._assert().NotNull()));
      var sequenceNumber = long.Parse(Request.Headers[HttpConstants.Headers.DeliveryStreamSequenceNumber][0]._assert().NotNull(), System.Globalization.CultureInfo.InvariantCulture);
      var predecessorSequenceNumber = long.Parse(Request.Headers[HttpConstants.Headers.DeliveryStreamPredecessorSequenceNumber][0]._assert().NotNull(), System.Globalization.CultureInfo.InvariantCulture);
      return new DeliveryStreamPosition(senderEndpointId, sequenceNumber, predecessorSequenceNumber);
   }

   ///<summary>Handlers run outside the MVC request's execution context, so no ambient request state flows into them — the same<br/>
   /// clean environment the named-pipe transport server gives the very same handlers.</summary>
   static Task<string> RunOutsideRequestContext(Func<Task<string>> handleRequest)
   {
      using(ExecutionContext.SuppressFlow())
         return Task.Run(handleRequest);
   }
}
