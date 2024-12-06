using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Compze.Messaging.Buses.Implementation;
using Compze.SystemCE.ThreadingCE.ResourceAccess;
using Compze.SystemCE.TransactionsCE;
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace Compze.Persistence.InMemory.ServiceBus;

class InMemoryInboxPersistenceLayer : IServiceBusPersistenceLayer.IInboxPersistenceLayer
{
   readonly IThreadShared<Implementation> _implementation = ThreadShared.WithDefaultTimeout(new Implementation());

   public void SaveMessage(Guid messageId, Guid typeId, string serializedMessage) => _implementation.Update(it => it.SaveMessage(messageId, typeId, serializedMessage));

   public void MarkAsSucceeded(Guid messageId)
      => Transaction.Current!.AddCommitTasks(() => _implementation.Update(it => it.MarkAsSucceeded(messageId)));

   public int RecordException(Guid messageId, string exceptionStackTrace, string exceptionMessage, string exceptionType)
      => _implementation.Update(it => it.RecordException(messageId, exceptionStackTrace, exceptionMessage, exceptionType));

   public int MarkAsFailed(Guid messageId) => _implementation.Update(it => it.MarkAsFailed(messageId));

   public Task InitAsync() => _implementation.Update(it => it.InitAsync());

   class Implementation : IServiceBusPersistenceLayer.IInboxPersistenceLayer
   {
      readonly List<Row> _rows = [];

      public void SaveMessage(Guid messageId, Guid typeId, string serializedMessage) => _rows.Add(new Row(messageId));

      public void MarkAsSucceeded(Guid messageId) => _rows.Single(it => it.MessageId == messageId).Status = Inbox.MessageStatus.Succeeded;

      public int RecordException(Guid messageId, string exceptionStackTrace, string exceptionMessage, string exceptionType)
      {
         var message = _rows.Single(it => it.MessageId == messageId);
         message.Status = Inbox.MessageStatus.Succeeded;
         message.ExceptionMessage = exceptionMessage;
         message.ExceptionStackTrace = exceptionStackTrace;
         message.ExceptionType = exceptionType;
         return 1;
      }

      public int MarkAsFailed(Guid messageId)
      {
         _rows.Single(it => it.MessageId == messageId).Status = Inbox.MessageStatus.Failed;
         return 1;
      }

      public Task InitAsync() => Task.CompletedTask;

      class Row(Guid messageId)
      {
         public Guid MessageId { get; } = messageId;

         public Inbox.MessageStatus Status { get; set; } = Inbox.MessageStatus.UnHandled;

         public string ExceptionMessage { get; set; } = string.Empty;
         public string ExceptionType { get; set;} = string.Empty;
         public string ExceptionStackTrace { get; set; } = string.Empty;
      }
   }
}