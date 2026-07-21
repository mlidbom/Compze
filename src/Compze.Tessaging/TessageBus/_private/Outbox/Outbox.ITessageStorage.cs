using Compze.Tessaging.Endpoints;
using Compze.Tessaging._internal.SqlLayer;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus._private.Outbox;

// ReSharper disable once ClassCannotBeInstantiated rider is plain confused
partial class Outbox
{
   public interface ITessageStorage
   {
      ///<summary>Persists the tessage with one dispatching row per receiver, in the caller's save transaction — see<br/>
      /// <see cref="ITessagingSqlLayer.IOutboxSqlLayer.SaveTessageAsync"/>. Returns each receiver's assigned delivery stream<br/>
      /// sequence number, which the commit hook hands the connection's exactly-once stream.</summary>
      Task<IReadOnlyDictionary<EndpointId, long>> SaveTessageAsync(ITessage tessage, TessageId dedupId, params EndpointId[] receiverEndpointIds);

      ///<summary>The delivery attempt's declared predecessor — see<br/>
      /// <see cref="ITessagingSqlLayer.IOutboxSqlLayer.GetDeliveryStreamPredecessorSequenceNumberAsync"/>.</summary>
      Task<long> GetDeliveryStreamPredecessorSequenceNumberAsync(EndpointId receiverId, long sequenceNumber);
      Task MarkAsReceivedAsync(TessageId tessageId, EndpointId receiverId);
      Task RecordDeliveryFailureAsync(TessageId tessageId, EndpointId receiverId, Exception? exception);

      ///<summary>The endpoint's recovery backlog: every tessage bound to it and not yet received, in send order. Stranded<br/>
      /// tessages are excluded — a stranded tommand waits for explicit resolution on the decommission surface, never for delivery.</summary>
      Task<IReadOnlyList<ITessagingSqlLayer.UndeliveredTessage>> GetUndeliveredTessagesForEndpointAsync(EndpointId endpointId);

      ///<summary>Discards these undelivered tessages bound to <paramref name="endpointId"/> — they will never be delivered:<br/>
      /// the fate of undelivered tevents whose subscriber renounced its subscription in a shrunk advertisement.</summary>
      Task DiscardUndeliveredTessagesAsync(EndpointId endpointId, IReadOnlyList<TessageId> tessageIds);

      ///<summary>Marks these undelivered tessages bound to <paramref name="endpointId"/> stranded: kept, but excluded from the<br/>
      /// recovery backlog until explicitly resolved on the decommission surface — the fate of undelivered tommands whose bound<br/>
      /// receiver's shrunk advertisement no longer handles their type.</summary>
      Task StrandUndeliveredTessagesAsync(EndpointId endpointId, IReadOnlyList<TessageId> tessageIds);

      ///<summary>Discards everything still owed to <paramref name="endpointId"/>, stranded tessages included, returning what was<br/>
      /// discarded so the caller can report it: the storage half of decommissioning a peer, and of the first-contact sweep.</summary>
      Task<IReadOnlyList<ITessagingSqlLayer.DiscardedTessage>> DiscardAllTessagesOwedToAsync(EndpointId endpointId);

      Task StartAsync();
   }
}
