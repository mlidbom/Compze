using Compze.Core.Public;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Tessaging.Hosting.Public;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Compze.Core.Tessaging.Internal.SqlLayer;

interface IServiceBusSqlLayer
{

   interface IOutboxSqlLayer
   {
      void SaveTessage(OutboxTessageWithReceivers tessageWithReceivers);
      MarkAsReceivedResult MarkAsReceived(TessageId tessageId, EndpointId endpointId);
      void RecordDeliveryFailure(TessageId tessageId, EndpointId endpointId, string failureReason);
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
      SaveTessageResult SaveTessage(TessageId tessageId, TypeId typeId, string serializedTessage);
      void MarkAsSucceeded(TessageId tessageId);
      int RecordException(TessageId tessageId, string exceptionStackTrace, string exceptionTessage, string exceptionType);
      int MarkAsFailed(TessageId tessageId);
      Task InitAsync();
   }

   class OutboxTessageWithReceivers(string serializedTessage, TypeId typeId, TessageId tessageId, IEnumerable<EndpointId> receiverEndpointIds)
   {
      public string SerializedTessage { get; } = serializedTessage;
      public TypeId TypeId { get; } = typeId;
      public TessageId TessageId { get; } = tessageId;
      public IEnumerable<EndpointId> ReceiverEndpointIds { get; } = receiverEndpointIds.ToList();
   }

   class UndeliveredTessage(TessageId tessageId, TypeId typeId, string serializedTessage, EndpointId targetEndpointId, int retryCount, DateTime? lastAttemptTime)
   {
      public TessageId TessageId { get; } = tessageId;
      public TypeId TypeId { get; } = typeId;
      public string SerializedTessage { get; } = serializedTessage;
      public EndpointId TargetEndpointId { get; } = targetEndpointId;
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