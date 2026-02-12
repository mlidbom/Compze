using Compze.Core.Public;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Tessaging.Hosting.Public;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Compze.Core.Tessaging.Internal.SqlLayer;

public interface IServiceBusSqlLayer
{

   public interface IOutboxSqlLayer
   {
      void SaveTessage(OutboxTessageWithReceivers tessageWithReceivers);
      MarkAsReceivedResult MarkAsReceived(TessageId tessageId, EndpointId endpointId);
      void RecordDeliveryFailure(TessageId tessageId, EndpointId endpointId, string failureReason);
      IReadOnlyList<UndeliveredTessage> GetUndeliveredTessages(TimeSpan olderThan);
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
      void MarkAsSucceeded(TessageId tessageId);
      int RecordException(TessageId tessageId, string exceptionStackTrace, string exceptionTessage, string exceptionType);
      int MarkAsFailed(TessageId tessageId);
      Task InitAsync();
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
      public const string TypeIdGuidValue = nameof(TypeIdGuidValue);
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
}