using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Public;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging.Transport.SqlLayer;

public interface IServiceBusSqlLayer
{

   public interface IOutboxSqlLayer
   {
      void SaveTessage(OutboxTessageWithReceivers tessageWithReceivers);
      MarkAsReceivedResult MarkAsReceived(TessageId tessageId, EndpointId endpointId);
      void RecordDeliveryFailure(TessageId tessageId, EndpointId endpointId, string failureReason);
      IReadOnlyList<UndeliveredTessage> GetUndeliveredTessagesForEndpoint(EndpointId endpointId);
      Task InitAsync();
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

      Task InitAsync();
   }

   ///<summary>One remembered peer as persisted: its identity and its last-known advertisement.</summary>
   public class PersistedPeer(EndpointId id, IReadOnlySet<string> handledTessageTypes)
   {
      public EndpointId Id { get; } = id;

      ///<summary>The canonical type-id strings of the remotable tessage types the peer advertised — the same strings its<br/>
      /// <see cref="Implementation.Transport.TessagingEndpointInformation.HandledTessageTypes"/> carries on the wire.</summary>
      public IReadOnlySet<string> HandledTessageTypes { get; } = handledTessageTypes;
   }

   public class OutboxTessageWithReceivers(string serializedTessage, TypeId typeId, TessageId tessageId, IEnumerable<EndpointId> receiverEndpointIds)
   {
      public string SerializedTessage { get; } = serializedTessage;
      public TypeId TypeId { get; } = typeId;
      public TessageId TessageId { get; } = tessageId;
      public IEnumerable<EndpointId> ReceiverEndpointIds { get; } = receiverEndpointIds.ToList();
   }

   public class UndeliveredTessage(TessageId tessageId, TypeId typeId, string serializedTessage, EndpointId targetEndpointId, int retryCount, DateTime? lastAttemptTime)
   {
      public TessageId TessageId { get; } = tessageId;
      public TypeId TypeId { get; } = typeId;
      public string SerializedTessage { get; } = serializedTessage;
      public EndpointId TargetEndpointId { get; } = targetEndpointId;
      public int RetryCount { get; } = retryCount;
      public DateTime? LastAttemptTime { get; } = lastAttemptTime;
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
