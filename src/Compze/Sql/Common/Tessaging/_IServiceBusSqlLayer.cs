using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Compze.Tessaging.Hosting.Implementation;

interface IServiceBusSqlLayer
{

   interface IOutboxSqlLayer
   {
      void SaveTessage(OutboxTessageWithReceivers tessageWithReceivers);
      MarkAsReceivedResult MarkAsReceived(Guid tessageId, Guid endpointId);
      void RecordDeliveryFailure(Guid tessageId, Guid endpointId, string failureReason);
      IReadOnlyList<UndeliveredTessage> GetUndeliveredTessages(TimeSpan olderThan);
      Task InitAsync();
   }

   enum MarkAsReceivedResult
   {
      Initial,
      WasAlreadyMarked
   }

   enum SaveTessageResult
   {
      NewTessage,
      Duplicate
   }

   interface IInboxSqlLayer
   {
      SaveTessageResult SaveTessage(Guid tessageId, Guid typeId, string serializedTessage);
      void MarkAsSucceeded(Guid tessageId);
      int RecordException(Guid tessageId, string exceptionStackTrace, string exceptionTessage, string exceptionType);
      int MarkAsFailed(Guid tessageId);
      Task InitAsync();
   }

   class OutboxTessageWithReceivers(string serializedTessage, Guid typeIdGuidValue, Guid tessageId, IEnumerable<Guid> receiverEndpointIds)
   {
      public string SerializedTessage { get; } = serializedTessage;
      public Guid TypeIdGuidValue { get; } = typeIdGuidValue;
      public Guid TessageId { get; } = tessageId;
      public IEnumerable<Guid> ReceiverEndpointIds { get; } = receiverEndpointIds.ToList();
   }

   class UndeliveredTessage(Guid tessageId, Guid typeIdGuid, string serializedTessage, Guid targetEndpointId, int retryCount, DateTime? lastAttemptTime)
   {
      public Guid TessageId { get; } = tessageId;
      public Guid TypeIdGuid { get; } = typeIdGuid;
      public string SerializedTessage { get; } = serializedTessage;
      public Guid TargetEndpointId { get; } = targetEndpointId;
      public int RetryCount { get; } = retryCount;
      public DateTime? LastAttemptTime { get; } = lastAttemptTime;
   }

   static class InboxTessageDatabaseSchemaStrings
   {
      internal const string TableName = "InboxTessages";

      internal const string GeneratedId = nameof(GeneratedId);
      internal const string TypeId = nameof(TypeId);
      internal const string TessageId = nameof(TessageId);
      internal const string Body = nameof(Body);
      public const string Status = nameof(Status);
      public const string ExceptionCount = nameof(ExceptionCount);
      public const string ExceptionTessage = nameof(ExceptionTessage);
      public const string ExceptionType = nameof(ExceptionType);
      public const string ExceptionStackTrace = nameof(ExceptionStackTrace);
   }

   static class OutboxTessagesDatabaseSchemaStrings
   {
      internal const string TableName = "OutboxTessages";

      internal const string GeneratedId = nameof(GeneratedId);
      internal const string TypeIdGuidValue = nameof(TypeIdGuidValue);
      internal const string TessageId = nameof(TessageId);
      internal const string SerializedTessage = nameof(SerializedTessage);
   }

   static class OutboxTessageDispatchingTableSchemaStrings
   {
      internal const string TableName = "OutboxTessageDispatching";

      internal const string TessageId = nameof(TessageId);
      internal const string EndpointId = nameof(EndpointId);
      internal const string IsReceived = nameof(IsReceived);
      internal const string RetryCount = nameof(RetryCount);
      internal const string LastAttemptTime = nameof(LastAttemptTime);
      internal const string FailureReason = nameof(FailureReason);
   }
}