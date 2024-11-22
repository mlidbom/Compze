using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using Composable.Messaging.Buses.Implementation;
using Composable.SystemCE.CollectionsCE.GenericCE;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.SystemCE.TransactionsCE;
using Message = Composable.Messaging.Buses.Implementation.IServiceBusPersistenceLayer.OutboxMessageWithReceivers;
// ReSharper disable CollectionNeverQueried.Local

namespace Composable.Persistence.InMemory.ServiceBus;

class InMemoryOutboxPersistenceLayer : IServiceBusPersistenceLayer.IOutboxPersistenceLayer
{
   readonly IThreadShared<Implementation> _implementation = ThreadShared.WithDefaultTimeout(new Implementation());

   public void SaveMessage(Message messageWithReceivers)
      => Transaction.Current!.AddCommitTasks(() => _implementation.Update(it => it.SaveMessage(messageWithReceivers)));
   public int MarkAsReceived(Guid messageId, Guid endpointId) => _implementation.Update(it => it.MarkAsReceived(messageId, endpointId));
   public Task InitAsync() => _implementation.Update(it => it.InitAsync());

   class Implementation : IServiceBusPersistenceLayer.IOutboxPersistenceLayer
   {
      readonly List<Message> _messages = [];
      readonly Dictionary<Guid, Dictionary<Guid, bool>> _dispatchingStatus = new();

      public void SaveMessage(Message messageWithReceivers)
      {
         _messages.Add(messageWithReceivers);
         var dispatchingInfo =_dispatchingStatus.GetOrAdd(messageWithReceivers.MessageId, () => new Dictionary<Guid, bool>());
         messageWithReceivers.ReceiverEndpointIds.ForEach(it => dispatchingInfo[it] = false);
      }

      public int MarkAsReceived(Guid messageId, Guid endpointId)
      {
         _dispatchingStatus[messageId][endpointId] = true;
         return 1;
      }

      public Task InitAsync() => Task.CompletedTask;
   }
}