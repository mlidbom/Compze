using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Peers._internal;
using Compze.Tessaging._internal.Transport;
using Compze.Tessaging._internal.Transport.Advertisement;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging._internal.SqlLayer;

interface ITessagingSqlLayer
{
   public interface IOutboxSqlLayer
   {
      ///<summary>Persists the tessage with one dispatching row per receiver, bound at save: a tevent's remembered subscribers,<br/>
      /// a tommand's one resolved receiver. Every tessage between a sender and a receiver thereby rides that pair's single<br/>
      /// ordered, receiver-deduped delivery stream — what makes exactly-once in-order hold by construction.<br/>
      /// Each dispatching row is assigned its <see cref="DeliveryStreamPosition"/> sequence number here, in the caller's save<br/>
      /// transaction, from the pair's counter row (<see cref="OutboxDeliveryStreamCountersSchemaStrings"/>) — whose lock<br/>
      /// serializes the pair's commits, so sequence order is commit order. Returns each receiver's assigned sequence number,<br/>
      /// which the commit hook hands the connection's delivery stream and the wire envelope carries to the receiver's inbox door.</summary>
      Task<IReadOnlyDictionary<EndpointId, long>> SaveTessageAsync(OutboxTessageWithReceivers tessageWithReceivers);

      ///<summary>The declared predecessor for a delivery attempt of the dispatching row at <paramref name="sequenceNumber"/><br/>
      /// bound to <paramref name="receiverId"/>: the pair's largest lower sequence number that is still deliverable or already<br/>
      /// received — 0 when none, meaning the tessage leads its stream. Sender-side pruning is excluded: a discarded row is<br/>
      /// gone and an unreceived stranded row awaits explicit resolution, so neither will ever reach the receiver's door —<br/>
      /// which admits a tessage exactly when its admission high-water mark equals this declared predecessor<br/>
      /// (see <see cref="DeliveryStreamPosition"/>). Computed fresh per delivery attempt, because pruning between attempts<br/>
      /// moves it.</summary>
      Task<long> GetDeliveryStreamPredecessorSequenceNumberAsync(EndpointId receiverId, long sequenceNumber);

      Task<MarkAsReceivedResult> MarkAsReceivedAsync(TessageId tessageId, EndpointId endpointId);

      Task RecordDeliveryFailureAsync(TessageId tessageId, EndpointId endpointId, string failureReason);

      ///<summary>The endpoint's recovery backlog: every tessage bound to <paramref name="endpointId"/> and not yet received,<br/>
      /// in the pair's delivery stream sequence order — which is commit order, so recovery re-establishes in-order delivery.<br/>
      /// Stranded tessages are excluded — a stranded tommand waits for explicit resolution on the decommission surface,<br/>
      /// never for delivery.</summary>
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
   /// report: its type, and whether it had been stranded (see <see cref="IOutboxSqlLayer.StrandUndeliveredTessagesAsync"/>)<br/>
   /// or was awaiting the peer's return.</summary>
   public class DiscardedTessage(TypeId typeId, bool wasStranded)
   {
      internal TypeId TypeId { get; } = typeId;
      internal bool WasStranded { get; } = wasStranded;
   }

   public enum MarkAsReceivedResult
   {
      Initial,
      WasAlreadyMarked
   }

   ///<summary>What the inbox door decided about an arriving tessage — see <see cref="IInboxSqlLayer.SaveTessageAsync"/>.</summary>
   public enum SaveTessageResult
   {
      ///<summary>Admitted: the tessage is the next in its pair's delivery stream and is now registered, awaiting handling.</summary>
      NewTessage,
      ///<summary>Already admitted earlier — a redelivery. The sender is acknowledged; nothing is registered or handled again.</summary>
      Duplicate,
      ///<summary>Refused: the tessage is ahead of its pair's delivery stream — its predecessor has not been admitted yet.<br/>
      /// The refusal travels back as the delivery's failure, and the sender's retry redelivers in order.</summary>
      RefusedAwaitingItsPredecessor
   }

   public interface IInboxSqlLayer
   {
      ///<summary>The inbox door: atomically admits the tessage iff it is the next in its pair's delivery stream — the pair's<br/>
      /// admission high-water mark (<see cref="InboxDeliveryStreamAdmissionsSchemaStrings"/>) must equal<br/>
      /// <paramref name="deliveryStreamPosition"/>'s declared predecessor — registering it durably in the same act.<br/>
      /// A tessage at or below the high-water mark is a redelivery, reported <see cref="SaveTessageResult.Duplicate"/>; one<br/>
      /// whose declared predecessor is not the mark is <see cref="SaveTessageResult.RefusedAwaitingItsPredecessor"/>.<br/>
      /// Advancing the high-water mark and inserting the row commit atomically, so exactly-once in-order admission holds by<br/>
      /// construction.</summary>
      Task<SaveTessageResult> SaveTessageAsync(TessageId tessageId, TypeId typeId, string serializedTessage, DeliveryStreamPosition deliveryStreamPosition);

      ///<summary>Claims the tessage's row for the caller's handling execution, exclusively: rides the caller's ambient<br/>
      /// handling transaction, taking a row-level claim held to its end, so the claim and the handler's work commit or roll<br/>
      /// back as one. False means the tessage is not this execution's to handle — its handling already finished<br/>
      /// (<see cref="InboxTessageDatabaseSchemaStrings.Status"/> is no longer UnHandled), or another live handling<br/>
      /// transaction holds the claim — and the caller skips without touching it.</summary>
      Task<bool> TryClaimForHandlingAsync(TessageId tessageId);

      ///<summary>The inbox's recovery backlog: every admitted tessage whose handling has not finished — status still<br/>
      /// UnHandled — in admission order (the inbox table's monotonic <see cref="InboxTessageDatabaseSchemaStrings.GeneratedId"/>;<br/>
      /// per pair that is stream order, since the admission gate serializes a pair's admissions). A hard crash between a<br/>
      /// tessage's admission and its handler-commit leaves exactly these rows, with no redelivery coming — the sender was<br/>
      /// acknowledged at admission — so the recovery scan at endpoint start re-enqueues them for handling.</summary>
      Task<IReadOnlyList<UnHandledTessage>> GetUnHandledTessagesAsync();

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

      ///<summary>The bound receivers, in one deterministic order (by <see cref="EndpointId"/>): every save locks its receivers'<br/>
      /// delivery stream counter rows in this order, so two concurrent saves to overlapping receiver sets can never deadlock<br/>
      /// on each other's counter rows.</summary>
      internal IReadOnlyList<EndpointId> ReceiverEndpointIds { get; } = [.. receiverEndpointIds.OrderBy(endpointId => endpointId.Value)];
   }

   ///<summary>One tessage the outbox still owes delivery of, as loaded into a connection's recovery backlog: exactly what<br/>
   /// re-enqueueing needs — identity for dedup, the tessage's sequence number in the pair's delivery stream, type for<br/>
   /// deserialization, and the serialized body.</summary>
   public class UndeliveredTessage(TessageId tessageId, long deliveryStreamSequenceNumber, TypeId typeId, string serializedTessage)
   {
      internal TessageId TessageId { get; } = tessageId;
      internal long DeliveryStreamSequenceNumber { get; } = deliveryStreamSequenceNumber;
      internal TypeId TypeId { get; } = typeId;
      internal string SerializedTessage { get; } = serializedTessage;
   }

   ///<summary>One tessage the inbox admitted but whose handling has not finished, as loaded by the recovery scan at endpoint<br/>
   /// start (see <see cref="IInboxSqlLayer.GetUnHandledTessagesAsync"/>): exactly what re-enqueueing for handling needs —<br/>
   /// identity, type for deserialization, and the serialized body.</summary>
   public class UnHandledTessage(TessageId tessageId, TypeId typeId, string serializedTessage)
   {
      internal TessageId TessageId { get; } = tessageId;
      internal TypeId TypeId { get; } = typeId;
      internal string SerializedTessage { get; } = serializedTessage;
   }

   ///<summary>The endpoint catalog's persistence: the one shared, unprefixed table every domain database carries — each<br/>
   /// endpoint's name, <see cref="EndpointId"/>, creation time, and — while one is running — a description of the process<br/>
   /// holding its process lock. It is what enforces name uniqueness and, through <see cref="TryTakeProcessLockAsync"/>, the<br/>
   /// one-process-per-endpoint rule, and what tells administration which endpoints inhabit the database.</summary>
   ///<remarks>The process lock is exclusivity a live holder holds — a database session for the server engines, an OS lock for<br/>
   /// the machine-local ones — never a time-bounded lease: no pause, however long, can lose it, and a dead process's lock is<br/>
   /// released by the infrastructure, so a restart after a crash claims the endpoint immediately. The recorded<br/>
   /// <see cref="EndpointCatalogEntry.LockHolderDescription"/> is advisory bookkeeping for error messages and administration;<br/>
   /// the lock itself is the enforcement.</remarks>
   public interface IEndpointCatalogSqlLayer
   {
      Task<EndpointCatalogEntry?> GetEntryByNameAsync(string endpointName);
      Task<EndpointCatalogEntry?> GetEntryByEndpointIdAsync(EndpointId endpointId);

      ///<summary>Creates the endpoint's catalog entry. False when an entry under <paramref name="endpointName"/> already<br/>
      /// exists, including one a racing process created this instant.</summary>
      Task<bool> TryInsertEntryAsync(string endpointName, EndpointId endpointId, DateTime utcNow);

      ///<summary>Takes the endpoint's process lock iff no other live process holds it. Null when another live process holds<br/>
      /// it — the claim is refused immediately: the holder holding proves it alive, so there is nothing to wait for.<br/>
      /// Disposing the returned hold releases the lock for the endpoint's next process; the holding process dying releases<br/>
      /// it too, through the infrastructure. <paramref name="onLockLostWhileHeld"/> reports the one way the lock can be<br/>
      /// lost without being released: the holding database session dying under a live holder — the domain database<br/>
      /// unreachable from this process.</summary>
      Task<IEndpointProcessLockHold?> TryTakeProcessLockAsync(string endpointName, Action<Exception> onLockLostWhileHeld);

      ///<summary>Records who holds the endpoint's process lock — the advisory bookkeeping the loud startup refusal and<br/>
      /// administration read; the lock itself is the enforcement.</summary>
      Task RecordLockHolderAsync(string endpointName, string lockHolderDescription);

      ///<summary>Clears the recorded lock holder — the clean-shutdown half of <see cref="RecordLockHolderAsync"/>. A crashed<br/>
      /// process's recorded holder lingers; that is harmless, because the lock, not the bookkeeping, decides.</summary>
      Task ClearLockHolderAsync(string endpointName);

      Task InitAsync();

      //todo: Decommissioning an endpoint's storage = dropping its prefixed table-set and deleting its catalog entry (refused
      //while its process lease is held). The design equation is settled (tessaging-target-design.md); the administration door
      //that performs the act - its surface, safety asserts, and report shape, mirroring PeerDecommissionReport - awaits its
      //first consumer.
   }

   ///<summary>The endpoint's held process lock (see <see cref="IEndpointCatalogSqlLayer.TryTakeProcessLockAsync"/>):<br/>
   /// disposal releases the lock for the endpoint's next process.</summary>
   public interface IEndpointProcessLockHold : IAsyncDisposable;

   ///<summary>One endpoint's row in the domain database's endpoint catalog: its identity and — advisory bookkeeping — who<br/>
   /// recorded itself as holding its process lock (see <see cref="IEndpointCatalogSqlLayer"/>).</summary>
   public class EndpointCatalogEntry(string endpointName, EndpointId endpointId, string? lockHolderDescription)
   {
      public string EndpointName { get; } = endpointName;
      internal EndpointId EndpointId { get; } = endpointId;

      ///<summary>Human-readable description of the process that recorded itself as the lock holder — null after a clean<br/>
      /// shutdown, possibly a crashed process's lingering record otherwise: the lock, not this bookkeeping, decides.</summary>
      internal string? LockHolderDescription { get; } = lockHolderDescription;
   }

   public static class EndpointCatalogDatabaseSchemaStrings
   {
      ///<summary>The catalog is domain-level data about the endpoints, inherently shared — the one deliberately unprefixed<br/>
      /// Tessaging table, unlike the per-endpoint table-sets (<see cref="EndpointTableSet"/>).</summary>
      internal const string TableName = "EndpointCatalog";

      public const string EndpointName = nameof(EndpointName);
      public const string EndpointId = nameof(EndpointId);
      public const string CreatedUtc = nameof(CreatedUtc);
      public const string LockHolderDescription = nameof(LockHolderDescription);
   }

   public static class InboxTessageDatabaseSchemaStrings
   {
      public const string GeneratedId = nameof(GeneratedId);
      public const string TypeId = nameof(TypeId);
      public const string TessageId = nameof(TessageId);
      public const string SenderEndpointId = nameof(SenderEndpointId);
      public const string DeliveryStreamSequenceNumber = nameof(DeliveryStreamSequenceNumber);
      public const string Body = nameof(Body);
      public const string Status = nameof(Status);
      public const string ExceptionCount = nameof(ExceptionCount);
      public const string ExceptionTessage = nameof(ExceptionTessage);
      public const string ExceptionType = nameof(ExceptionType);
      public const string ExceptionStackTrace = nameof(ExceptionStackTrace);
   }

   ///<summary>The inbox's per-sender admission high-water marks: one row per sender peer, holding the sequence number of the<br/>
   /// last tessage admitted from that pair's delivery stream. The inbox door admits a tessage exactly when the mark equals<br/>
   /// the tessage's declared predecessor (see <see cref="DeliveryStreamPosition"/>), first contact starting from a declared<br/>
   /// predecessor of 0 — see <see cref="IInboxSqlLayer.SaveTessageAsync"/>.</summary>
   public static class InboxDeliveryStreamAdmissionsSchemaStrings
   {
      public const string SenderEndpointId = nameof(SenderEndpointId);
      public const string LastAdmittedSequenceNumber = nameof(LastAdmittedSequenceNumber);
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
      public const string DeliveryStreamSequenceNumber = nameof(DeliveryStreamSequenceNumber);
      public const string IsReceived = nameof(IsReceived);
      public const string IsStranded = nameof(IsStranded);
      public const string RetryCount = nameof(RetryCount);
      public const string LastAttemptTime = nameof(LastAttemptTime);
      public const string FailureReason = nameof(FailureReason);
   }

   ///<summary>The outbox's per-receiver delivery stream counters: one row per receiver peer, holding the last sequence number<br/>
   /// assigned in that pair's delivery stream. <see cref="IOutboxSqlLayer.SaveTessageAsync"/> increments it inside the save<br/>
   /// transaction to assign each dispatching row its <see cref="OutboxTessageDispatchingTableSchemaStrings.DeliveryStreamSequenceNumber"/>;<br/>
   /// the counter row's lock is what serializes the pair's commits, making sequence order commit order.</summary>
   public static class OutboxDeliveryStreamCountersSchemaStrings
   {
      public const string EndpointId = nameof(EndpointId);
      public const string LastAssignedSequenceNumber = nameof(LastAssignedSequenceNumber);
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
