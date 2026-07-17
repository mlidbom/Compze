using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Public;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging.Transport.SqlLayer;

public interface IServiceBusSqlLayer
{

   public interface IOutboxSqlLayer
   {
      ///<summary>Persists the tessage with one dispatching row per receiver, bound at save: a tevent's remembered subscribers,<br/>
      /// a tommand's one resolved receiver. Every tessage between a sender and a receiver thereby rides that pair's single<br/>
      /// ordered, receiver-deduped delivery stream — what makes exactly-once in-order hold by construction.</summary>
      void SaveTessage(OutboxTessageWithReceivers tessageWithReceivers);

      MarkAsReceivedResult MarkAsReceived(TessageId tessageId, EndpointId endpointId);

      void RecordDeliveryFailure(TessageId tessageId, EndpointId endpointId, string failureReason);

      ///<summary>The endpoint's recovery backlog: every tessage bound to <paramref name="endpointId"/> and not yet received,<br/>
      /// in send order (the outbox tessage table's monotonic <c>GeneratedId</c>). Stranded tessages are excluded — a stranded<br/>
      /// tommand waits for explicit resolution on the decommission surface, never for delivery.</summary>
      IReadOnlyList<UndeliveredTessage> GetUndeliveredTessagesForEndpoint(EndpointId endpointId);

      ///<summary>Discards these undelivered tessages bound to <paramref name="endpointId"/>: their dispatching rows are deleted,<br/>
      /// so they will never be delivered — the fate of undelivered tevents whose subscriber renounced its subscription in a<br/>
      /// shrunk advertisement. Runs in the caller's ambient transaction.</summary>
      void DiscardUndeliveredTessages(EndpointId endpointId, IReadOnlyList<TessageId> tessageIds);

      ///<summary>Marks these undelivered tessages bound to <paramref name="endpointId"/> stranded: kept, but excluded from the<br/>
      /// recovery backlog — the fate of undelivered tommands whose bound receiver's shrunk advertisement no longer handles their<br/>
      /// type. A stranded tommand is resolved explicitly on the decommission surface. Runs in the caller's ambient transaction.</summary>
      void StrandUndeliveredTessages(EndpointId endpointId, IReadOnlyList<TessageId> tessageIds);

      ///<summary>Discards everything still owed to <paramref name="endpointId"/> — every unreceived dispatching row bound to it,<br/>
      /// stranded ones included — returning what was discarded, so the caller can report it: the storage half of decommissioning<br/>
      /// a peer, and of the first-contact sweep. Runs in the caller's ambient transaction.</summary>
      IReadOnlyList<DiscardedTessage> DiscardAllTessagesOwedTo(EndpointId endpointId);

      Task InitAsync();
   }

   ///<summary>One tessage discarded by <see cref="IOutboxSqlLayer.DiscardAllTessagesOwedTo"/>, described for the discarder's<br/>
   /// report: its identity, its type, and whether it had been stranded (see <see cref="IOutboxSqlLayer.StrandUndeliveredTessages"/>)<br/>
   /// or was awaiting the peer's return.</summary>
   public class DiscardedTessage(TessageId tessageId, TypeId typeId, bool wasStranded)
   {
      public TessageId TessageId { get; } = tessageId;
      public TypeId TypeId { get; } = typeId;
      public bool WasStranded { get; } = wasStranded;
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
      SaveTessageResult SaveTessage(TessageId tessageId, TypeId typeId, string serializedTessage);
      int MarkAsSucceeded(TessageId tessageId);
      int RecordException(TessageId tessageId, string exceptionStackTrace, string exceptionTessage, string exceptionType);
      int MarkAsFailed(TessageId tessageId);
      Task InitAsync();
   }

   ///<summary>The peer registry's persistence: the endpoint's durable memory of its peers and their last-known advertisements<br/>
   /// (see <see cref="Implementation.Peers.IPeerRegistry"/> and <c>dev_docs/TODO/durable-peer-topology.md</c>).</summary>
   public interface IPeerRegistrySqlLayer
   {
      ///<summary>Replaces <paramref name="peerId"/>'s stored advertisement wholesale — creating the peer on first contact.<br/>
      /// The caller runs it inside its own transaction, so a reader never sees a half-replaced advertisement.</summary>
      void SaveAdvertisement(EndpointId peerId, IReadOnlySet<string> handledTessageTypes);

      ///<summary>Every remembered peer, with its stored advertisement.</summary>
      IReadOnlyList<PersistedPeer> GetPeers();

      ///<summary>Deletes <paramref name="peerId"/>'s row and stored advertisement — the durable half of decommissioning the<br/>
      /// peer. Runs in the caller's ambient transaction: the whole decommission act commits or rolls back together.</summary>
      void DeletePeer(EndpointId peerId);

      Task InitAsync();
   }

   ///<summary>One remembered peer as persisted: its identity and its last-known advertisement.</summary>
   public class PersistedPeer(EndpointId id, IReadOnlySet<string> handledTessageTypes)
   {
      public EndpointId Id { get; } = id;

      //todo: We seem to always serialize and persist TypeIds as nothing more than strings. We should have a value type for this.
      ///<summary>The canonical type-id strings of the remotable tessage types the peer advertised — the same strings its<br/>
      /// <see cref="Implementation.Transport.TessagingEndpointInformation.HandledTessageTypes"/> carries on the wire.</summary>
      public IReadOnlySet<string> HandledTessageTypes { get; } = handledTessageTypes;
   }

   public class OutboxTessageWithReceivers(string serializedTessage, TypeId typeId, TessageId tessageId, IEnumerable<EndpointId> receiverEndpointIds)
   {
      public string SerializedTessage { get; } = serializedTessage;
      public TypeId TypeId { get; } = typeId;
      public TessageId TessageId { get; } = tessageId;
      public IEnumerable<EndpointId> ReceiverEndpointIds { get; } = [..receiverEndpointIds];
   }

   ///<summary>One tessage the outbox still owes delivery of, as loaded into a connection's recovery backlog: exactly what<br/>
   /// re-enqueueing needs — identity for dedup, type for deserialization, and the serialized body.</summary>
   public class UndeliveredTessage(TessageId tessageId, TypeId typeId, string serializedTessage)
   {
      public TessageId TessageId { get; } = tessageId;
      public TypeId TypeId { get; } = typeId;
      public string SerializedTessage { get; } = serializedTessage;
   }

   public static class InboxTessageDatabaseSchemaStrings
   {
      public const string TableName = "InboxTessages";

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
      public const string TableName = "OutboxTessages";

      public const string GeneratedId = nameof(GeneratedId);
      public const string TypeId = nameof(TypeId);
      public const string TessageId = nameof(TessageId);
      public const string SerializedTessage = nameof(SerializedTessage);
   }

   public static class OutboxTessageDispatchingTableSchemaStrings
   {
      public const string TableName = "OutboxTessageDispatching";

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
      public const string TableName = "Peers";

      public const string EndpointId = nameof(EndpointId);
   }

   public static class PeerHandledTessageTypesDatabaseSchemaStrings
   {
      public const string TableName = "PeerHandledTessageTypes";

      public const string EndpointId = nameof(EndpointId);
      public const string HandledTessageType = nameof(HandledTessageType);
   }
}
