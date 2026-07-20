using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Peers.Internal;
using Compze.Tessaging.Internal.Transport.Advertisement;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging.Internal.SqlLayer;

public interface ITessagingSqlLayer
{
   public interface IOutboxSqlLayer
   {
      ///<summary>Persists the tessage with one dispatching row per receiver, bound at save: a tevent's remembered subscribers,<br/>
      /// a tommand's one resolved receiver. Every tessage between a sender and a receiver thereby rides that pair's single<br/>
      /// ordered, receiver-deduped delivery stream — what makes exactly-once in-order hold by construction.</summary>
      Task SaveTessageAsync(OutboxTessageWithReceivers tessageWithReceivers);

      Task<MarkAsReceivedResult> MarkAsReceivedAsync(TessageId tessageId, EndpointId endpointId);

      Task RecordDeliveryFailureAsync(TessageId tessageId, EndpointId endpointId, string failureReason);

      ///<summary>The endpoint's recovery backlog: every tessage bound to <paramref name="endpointId"/> and not yet received,<br/>
      /// in send order (the outbox tessage table's monotonic <c>GeneratedId</c>). Stranded tessages are excluded — a stranded<br/>
      /// tommand waits for explicit resolution on the decommission surface, never for delivery.</summary>
      Task<IReadOnlyList<UndeliveredTessage>> GetUndeliveredTessagesForEndpointAsync(EndpointId endpointId);

      ///<summary>Discards these undelivered tessages bound to <paramref name="endpointId"/>: their dispatching rows are deleted,<br/>
      /// so they will never be delivered — the fate of undelivered tevents whose subscriber renounced its subscription in a<br/>
      /// shrunk advertisement. Runs in the caller's ambient transaction.</summary>
      Task DiscardUndeliveredTessagesAsync(EndpointId endpointId, IReadOnlyList<TessageId> tessageIds);

      ///<summary>Marks these undelivered tessages bound to <paramref name="endpointId"/> stranded: kept, but excluded from the<br/>
      /// recovery backlog — the fate of undelivered tommands whose bound receiver's shrunk advertisement no longer handles their<br/>
      /// type. A stranded tommand is resolved explicitly on the decommission surface. Runs in the caller's ambient transaction.</summary>
      Task StrandUndeliveredTessagesAsync(EndpointId endpointId, IReadOnlyList<TessageId> tessageIds);

      ///<summary>Discards everything still owed to <paramref name="endpointId"/> — every unreceived dispatching row bound to it,<br/>
      /// stranded ones included — returning what was discarded, so the caller can report it: the storage half of decommissioning<br/>
      /// a peer, and of the first-contact sweep. Runs in the caller's ambient transaction.</summary>
      Task<IReadOnlyList<DiscardedTessage>> DiscardAllTessagesOwedToAsync(EndpointId endpointId);

      Task InitAsync();
   }

   ///<summary>One tessage discarded by <see cref="IOutboxSqlLayer.DiscardAllTessagesOwedToAsync"/>, described for the discarder's<br/>
   /// report: its identity, its type, and whether it had been stranded (see <see cref="IOutboxSqlLayer.StrandUndeliveredTessagesAsync"/>)<br/>
   /// or was awaiting the peer's return.</summary>
   public class DiscardedTessage(TessageId tessageId, TypeId typeId, bool wasStranded)
   {
      public TessageId TessageId { get; } = tessageId;
      internal TypeId TypeId { get; } = typeId;
      internal bool WasStranded { get; } = wasStranded;
   }

   public enum MarkAsReceivedResult
   {
      Initial,
      WasAlreadyMarked
   }

   public enum SaveTessageResult
   {
      NewTessage,
      Duplicate
   }

   public interface IInboxSqlLayer
   {
      Task<SaveTessageResult> SaveTessageAsync(TessageId tessageId, TypeId typeId, string serializedTessage);
      Task<int> MarkAsSucceededAsync(TessageId tessageId);
      Task<int> RecordExceptionAsync(TessageId tessageId, string exceptionStackTrace, string exceptionTessage, string exceptionType);
      Task<int> MarkAsFailedAsync(TessageId tessageId);
      Task InitAsync();
   }

   ///<summary>The peer registry's persistence: the endpoint's durable memory of its peers and their last-known advertisements<br/>
   /// (see <see cref="IPeerRegistry"/> and <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>).</summary>
   public interface IPeerRegistrySqlLayer
   {
      ///<summary>Replaces <paramref name="peerId"/>'s stored advertisement wholesale — creating the peer on first contact.<br/>
      /// The caller runs it inside its own transaction, so a reader never sees a half-replaced advertisement.</summary>
      Task SaveAdvertisementAsync(EndpointId peerId, IReadOnlySet<string> handledTessageTypes);

      ///<summary>Every remembered peer, with its stored advertisement.</summary>
      Task<IReadOnlyList<PersistedPeer>> GetPeersAsync();

      ///<summary>Deletes <paramref name="peerId"/>'s row and stored advertisement — the durable half of decommissioning the<br/>
      /// peer. Runs in the caller's ambient transaction: the whole decommission act commits or rolls back together.</summary>
      Task DeletePeerAsync(EndpointId peerId);

      Task InitAsync();
   }

   ///<summary>One remembered peer as persisted: its identity and its last-known advertisement.</summary>
   public class PersistedPeer(EndpointId id, IReadOnlySet<string> handledTessageTypes)
   {
      public EndpointId Id { get; } = id;

      //todo: We seem to always serialize and persist TypeIds as nothing more than strings. We should have a value type for this.
      ///<summary>The canonical type-id strings of the remotable tessage types the peer advertised — the same strings its<br/>
      /// <see cref="EndpointInformation.HandledTessageTypes"/> carries on the wire.</summary>
      public IReadOnlySet<string> HandledTessageTypes { get; } = handledTessageTypes;
   }

   public class OutboxTessageWithReceivers(string serializedTessage, TypeId typeId, TessageId tessageId, IEnumerable<EndpointId> receiverEndpointIds)
   {
      internal string SerializedTessage { get; } = serializedTessage;
      internal TypeId TypeId { get; } = typeId;
      internal TessageId TessageId { get; } = tessageId;
      internal IEnumerable<EndpointId> ReceiverEndpointIds { get; } = [.. receiverEndpointIds];
   }

   ///<summary>One tessage the outbox still owes delivery of, as loaded into a connection's recovery backlog: exactly what<br/>
   /// re-enqueueing needs — identity for dedup, type for deserialization, and the serialized body.</summary>
   public class UndeliveredTessage(TessageId tessageId, TypeId typeId, string serializedTessage)
   {
      internal TessageId TessageId { get; } = tessageId;
      internal TypeId TypeId { get; } = typeId;
      internal string SerializedTessage { get; } = serializedTessage;
   }

   ///<summary>The endpoint catalog's persistence: the one shared, unprefixed table every domain database carries — each<br/>
   /// endpoint's name, <see cref="EndpointId"/>, creation time, and process lease. It is what enforces name uniqueness and<br/>
   /// the one-process-per-endpoint rule, and what tells administration which endpoints inhabit the database.</summary>
   ///<remarks>The lease members are single conditional statements, so racing claimants serialize on the database and exactly<br/>
   /// one wins — no read-then-write window. Every timestamp is written by the caller's clock: staleness judgements assume the<br/>
   /// processes sharing a domain database keep reasonably synchronized clocks.</remarks>
   public interface IEndpointCatalogSqlLayer
   {
      Task<EndpointCatalogEntry?> GetEntryByNameAsync(string endpointName);
      Task<EndpointCatalogEntry?> GetEntryByEndpointIdAsync(EndpointId endpointId);

      ///<summary>Creates the endpoint's catalog entry with the process lease already held — the first-registration path.<br/>
      /// False when an entry under <paramref name="endpointName"/> already exists, including one a racing process created<br/>
      /// this instant.</summary>
      Task<bool> TryInsertEntryHoldingTheLeaseAsync(string endpointName, EndpointId endpointId, Guid leaseHolderId, string leaseHolderDescription, DateTime utcNow);

      ///<summary>Takes the process lease iff it is free or stale — unrefreshed since before <paramref name="staleBefore"/>.</summary>
      Task<bool> TryTakeTheLeaseAsync(string endpointName, Guid leaseHolderId, string leaseHolderDescription, DateTime utcNow, DateTime staleBefore);

      ///<summary>Refreshes the lease's heartbeat iff <paramref name="leaseHolderId"/> still holds it. False means the lease<br/>
      /// was taken from us: this process went unrefreshed past the lease duration and was presumed dead.</summary>
      Task<bool> TryHeartbeatAsync(string endpointName, Guid leaseHolderId, DateTime utcNow);

      ///<summary>Releases the lease iff <paramref name="leaseHolderId"/> still holds it — the clean-shutdown half; a crashed<br/>
      /// process's lease is instead taken over once stale.</summary>
      Task ReleaseTheLeaseAsync(string endpointName, Guid leaseHolderId);

      ///<summary>Every endpoint inhabiting the domain database.</summary>
      Task<IReadOnlyList<EndpointCatalogEntry>> GetEntriesAsync();

      Task InitAsync();

      //todo: Decommissioning an endpoint's storage = dropping its prefixed table-set and deleting its catalog entry (refused
      //while its process lease is held). The design equation is settled (tessaging-target-design.md); the administration door
      //that performs the act - its surface, safety asserts, and report shape, mirroring PeerDecommissionReport - awaits its
      //first consumer.
   }

   ///<summary>One endpoint's row in the domain database's endpoint catalog: its identity, when it first registered, and who —<br/>
   /// if anyone — holds its process lease right now.</summary>
   public class EndpointCatalogEntry(string endpointName, EndpointId endpointId, DateTime createdUtc, string? leaseHolderDescription, DateTime? leaseHeartbeatUtc)
   {
      public string EndpointName { get; } = endpointName;
      internal EndpointId EndpointId { get; } = endpointId;
      public DateTime CreatedUtc { get; } = createdUtc;

      ///<summary>Human-readable description of the process holding the lease — null when the lease is free.</summary>
      internal string? LeaseHolderDescription { get; } = leaseHolderDescription;

      internal DateTime? LeaseHeartbeatUtc { get; } = leaseHeartbeatUtc;
   }

   public static class EndpointCatalogDatabaseSchemaStrings
   {
      ///<summary>The catalog is domain-level data about the endpoints, inherently shared — the one deliberately unprefixed<br/>
      /// Tessaging table, unlike the per-endpoint table-sets (<see cref="EndpointTableSet"/>).</summary>
      internal const string TableName = "EndpointCatalog";

      public const string EndpointName = nameof(EndpointName);
      public const string EndpointId = nameof(EndpointId);
      public const string CreatedUtc = nameof(CreatedUtc);
      public const string LeaseHolderId = nameof(LeaseHolderId);
      public const string LeaseHolderDescription = nameof(LeaseHolderDescription);
      public const string LeaseHeartbeatUtc = nameof(LeaseHeartbeatUtc);
   }

   public static class InboxTessageDatabaseSchemaStrings
   {
      public const string GeneratedId = nameof(GeneratedId);
      public const string TypeId = nameof(TypeId);
      public const string TessageId = nameof(TessageId);
      public const string Body = nameof(Body);
      public const string Status = nameof(Status);
      public const string ExceptionCount = nameof(ExceptionCount);
      public const string ExceptionTessage = nameof(ExceptionTessage);
      public const string ExceptionType = nameof(ExceptionType);
      public const string ExceptionStackTrace = nameof(ExceptionStackTrace);
   }

   public static class OutboxTessagesDatabaseSchemaStrings
   {
      public const string GeneratedId = nameof(GeneratedId);
      public const string TypeId = nameof(TypeId);
      public const string TessageId = nameof(TessageId);
      public const string SerializedTessage = nameof(SerializedTessage);
   }

   public static class OutboxTessageDispatchingTableSchemaStrings
   {
      public const string TessageId = nameof(TessageId);
      public const string EndpointId = nameof(EndpointId);
      public const string IsReceived = nameof(IsReceived);
      public const string IsStranded = nameof(IsStranded);
      public const string RetryCount = nameof(RetryCount);
      public const string LastAttemptTime = nameof(LastAttemptTime);
      public const string FailureReason = nameof(FailureReason);
   }

   public static class PeersDatabaseSchemaStrings
   {
      public const string EndpointId = nameof(EndpointId);
   }

   public static class PeerHandledTessageTypesDatabaseSchemaStrings
   {
      public const string EndpointId = nameof(EndpointId);
      public const string HandledTessageType = nameof(HandledTessageType);
   }
}
