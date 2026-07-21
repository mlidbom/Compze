using Compze.Tessaging.Endpoints.Discovery;

namespace Compze.Tessaging._private.Transport;

interface ITransportMessagePoster
{
   ///<summary>Posts <paramref name="tessage"/> to the receiving endpoint's transport server and awaits its acknowledgement.<br/>
   /// An exactly-once tessage's caller passes <paramref name="deliveryStreamPredecessorSequenceNumber"/> — the sequence number<br/>
   /// of the previous still-deliverable member of the pair's delivery stream, freshly computed for this attempt (see<br/>
   /// <see cref="Compze.Tessaging._internal.Transport.DeliveryStreamPosition"/>); the other tiers pass none.</summary>
   Task PostAsync(TransportTessage.OutGoing tessage, EndpointAddress endPointAddress, long? deliveryStreamPredecessorSequenceNumber = null, CancellationToken cancellationToken = default);
}
