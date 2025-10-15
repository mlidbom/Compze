using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Compze.Tessaging.Hosting.Implementation;

interface IServiceBusSqlLayer
{

   interface IOutboxSqlLayer
   {
      void SaveMessage(OutboxMessageWithReceivers messageWithReceivers);
      int MarkAsReceived(Guid messageId, Guid endpointId);
      void RecordDeliveryFailure(Guid messageId, Guid endpointId, string failureReason);
      IReadOnlyList<UndeliveredMessage> GetUndeliveredMessages(TimeSpan olderThan);
      Task InitAsync();
   }

   interface IInboxSqlLayer
   {
      void SaveMessage(Guid messageId, Guid typeId, string serializedMessage);
      void MarkAsSucceeded(Guid messageId);
      int RecordException(Guid messageId, string exceptionStackTrace, string exceptionMessage, string exceptionType);
      int MarkAsFailed(Guid messageId);
      Task InitAsync();
   }

   class OutboxMessageWithReceivers(string serializedMessage, Guid typeIdGuidValue, Guid messageId, IEnumerable<Guid> receiverEndpointIds)
   {
      public string SerializedMessage { get; } = serializedMessage;
      public Guid TypeIdGuidValue { get; } = typeIdGuidValue;
      public Guid MessageId { get; } = messageId;
      public IEnumerable<Guid> ReceiverEndpointIds { get; } = receiverEndpointIds.ToList();
   }

   class UndeliveredMessage(Guid messageId, Guid typeIdGuid, string serializedMessage, Guid targetEndpointId, int retryCount, DateTime? lastAttemptTime)
   {
      public Guid MessageId { get; } = messageId;
      public Guid TypeIdGuid { get; } = typeIdGuid;
      public string SerializedMessage { get; } = serializedMessage;
      public Guid TargetEndpointId { get; } = targetEndpointId;
      public int RetryCount { get; } = retryCount;
      public DateTime? LastAttemptTime { get; } = lastAttemptTime;
   }

   static class InboxMessageDatabaseSchemaStrings
   {
      internal const string TableName = "InboxMessages";

      internal const string GeneratedId = nameof(GeneratedId);
      internal const string TypeId = nameof(TypeId);
      internal const string MessageId = nameof(MessageId);
      internal const string Body = nameof(Body);
      public const string Status = nameof(Status);
      public const string ExceptionCount = nameof(ExceptionCount);
      public const string ExceptionMessage = nameof(ExceptionMessage);
      public const string ExceptionType = nameof(ExceptionType);
      public const string ExceptionStackTrace = nameof(ExceptionStackTrace);
   }

   static class OutboxMessagesDatabaseSchemaStrings
   {
      internal const string TableName = "OutboxMessages";

      internal const string GeneratedId = nameof(GeneratedId);
      internal const string TypeIdGuidValue = nameof(TypeIdGuidValue);
      internal const string MessageId = nameof(MessageId);
      internal const string SerializedMessage = nameof(SerializedMessage);
   }

   static class OutboxMessageDispatchingTableSchemaStrings
   {
      internal const string TableName = "OutboxMessageDispatching";

      internal const string MessageId = nameof(MessageId);
      internal const string EndpointId = nameof(EndpointId);
      internal const string IsReceived = nameof(IsReceived);
      internal const string RetryCount = nameof(RetryCount);
      internal const string LastAttemptTime = nameof(LastAttemptTime);
      internal const string FailureReason = nameof(FailureReason);
   }
}