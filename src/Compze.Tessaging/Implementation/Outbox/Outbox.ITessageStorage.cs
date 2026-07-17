using Compze.Abstractions.Public;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Transport.SqlLayer;

namespace Compze.Tessaging.Implementation.Outbox;

// ReSharper disable once ClassCannotBeInstantiated rider is plain confused
partial class Outbox
{
   public interface ITessageStorage
   {
      void SaveTessage(ITessage tessage, TessageId dedupId, params EndpointId[] receiverEndpointIds);
      void MarkAsReceived(TessageId tessageId, EndpointId receiverId);
      void RecordDeliveryFailure(TessageId tessageId, EndpointId receiverId, Exception? exception);

      ///<summary>The endpoint's recovery backlog: every tessage bound to it and not yet received, in send order. Stranded<br/>
      /// tessages are excluded — a stranded tommand waits for explicit resolution on the decommission surface, never for delivery.</summary>
      IReadOnlyList<IServiceBusSqlLayer.UndeliveredTessage> GetUndeliveredTessagesForEndpoint(EndpointId endpointId);

      ///<summary>Discards these undelivered tessages bound to <paramref name="endpointId"/> — they will never be delivered:<br/>
      /// the fate of undelivered tevents whose subscriber renounced its subscription in a shrunk advertisement.</summary>
      void DiscardUndeliveredTessages(EndpointId endpointId, IReadOnlyList<TessageId> tessageIds);

      ///<summary>Marks these undelivered tessages bound to <paramref name="endpointId"/> stranded: kept, but excluded from the<br/>
      /// recovery backlog until explicitly resolved on the decommission surface — the fate of undelivered tommands whose bound<br/>
      /// receiver's shrunk advertisement no longer handles their type.</summary>
      void StrandUndeliveredTessages(EndpointId endpointId, IReadOnlyList<TessageId> tessageIds);

      ///<summary>Discards everything still owed to <paramref name="endpointId"/>, stranded tessages included, returning what was<br/>
      /// discarded so the caller can report it: the storage half of decommissioning a peer, and of the first-contact sweep.</summary>
      IReadOnlyList<IServiceBusSqlLayer.DiscardedTessage> DiscardAllTessagesOwedTo(EndpointId endpointId);

      Task StartAsync();
   }
}
