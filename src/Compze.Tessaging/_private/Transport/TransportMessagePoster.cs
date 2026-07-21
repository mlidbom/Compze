using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging._internal.Transport;

namespace Compze.Tessaging._private.Transport;

static class TransportMessagePosterRegistrar
{
   ///<summary>Registers the client side of the Tessaging transport (<see cref="TransportMessagePoster"/>), which runs on the<br/>
   /// endpoint transport client (<see cref="IEndpointTransportClient"/>) a protocol registration supplies.</summary>
   public static IComponentRegistrar TessagingTransportMessagePoster(this IComponentRegistrar registrar)
      => registrar.Register(TransportMessagePoster.RegisterWith);
}

///<summary>The client side of the Tessaging transport: posts a tessage to the receiving endpoint's inbox through the endpoint<br/>
/// transport client (<see cref="IEndpointTransportClient"/>) and awaits the acknowledgement written after the inbox has<br/>
/// registered the tessage — one implementation for every protocol, since the protocol difference lives entirely in the<br/>
/// transport client.</summary>
class TransportMessagePoster : ITransportMessagePoster
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITransportMessagePoster>()
                                     .CreatedBy((IEndpointTransportClient transportClient, EndpointConfiguration configuration) => new TransportMessagePoster(transportClient, configuration)));

   readonly IEndpointTransportClient _transportClient;
   readonly EndpointConfiguration _configuration;

   TransportMessagePoster(IEndpointTransportClient transportClient, EndpointConfiguration configuration)
   {
      _transportClient = transportClient;
      _configuration = configuration;
   }

   public async Task PostAsync(TransportTessage.OutGoing tessage, EndpointAddress endPointAddress, long? deliveryStreamPredecessorSequenceNumber = null, CancellationToken cancellationToken = default) =>
      await _transportClient.SendAsync(new TransportRequest(RequestKindFor(tessage), tessage.TessageId, tessage.Type.CanonicalString, tessage.Body, DeliveryStreamPositionFor(tessage, deliveryStreamPredecessorSequenceNumber)),
                                       endPointAddress, cancellationToken).caf();

   ///<summary>The exactly-once kinds carry the tessage's <see cref="DeliveryStreamPosition"/> on the wire — this endpoint is<br/>
   /// the stream's sender, the outbox save assigned the sequence number, the delivery stream computed the attempt's<br/>
   /// predecessor — so the receiver's inbox door can admit in stream order.</summary>
   DeliveryStreamPosition? DeliveryStreamPositionFor(TransportTessage.OutGoing tessage, long? predecessorSequenceNumber) =>
      tessage.DeliveryStreamSequenceNumber is { } sequenceNumber
         ? new DeliveryStreamPosition(_configuration.Id, sequenceNumber, predecessorSequenceNumber._assert().NotNull().Value)
         : null;

   static TransportRequestKind RequestKindFor(TransportTessage.OutGoing tessage) =>
      tessage.TessageTypeEnum switch
      {
         TransportTessageType.ExactlyOnceTevent => TransportRequestKind.ExactlyOnceTevent,
         TransportTessageType.ExactlyOnceTommand => TransportRequestKind.ExactlyOnceTommand,
         TransportTessageType.BestEffortTevent => TransportRequestKind.BestEffortTevent,
         _ => throw new ArgumentOutOfRangeException()
      };
}
