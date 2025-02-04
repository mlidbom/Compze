﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Compze.Contracts;
using Compze.SystemCE.LinqCE;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.SystemCE.TransactionsCE;

namespace Compze.Messaging.Buses.Implementation;

partial class Outbox(ITransport transport, Outbox.IMessageStorage messageStorage, RealEndpointConfiguration configuration) : IOutbox
{
   readonly IMessageStorage _storage = messageStorage;
   readonly RealEndpointConfiguration _configuration = configuration;
   readonly ITransport _transport = transport;

   public void PublishTransactionally(IExactlyOnceEvent exactlyOnceEvent)
   {
      Assert.State.NotNull(Transaction.Current);
      var connections = _transport.SubscriberConnectionsFor(exactlyOnceEvent)
                                  .Where(connection => connection.EndpointInformation.Id != _configuration.Id)
                                  .ToArray(); //We dispatch events to ourselves synchronously so don't go doing it again here.;

      //Urgent: bug. Our traceability thinking does not allow just discarding this message.But removing this if statement breaks a lot of tests that uses endpoint wiring but do not start an endpoint.
      if(connections.Length != 0)
      {
         var eventHandlerEndpointIds = connections.Select(connection => connection.EndpointInformation.Id).ToArray();
         _storage.SaveMessage(exactlyOnceEvent, eventHandlerEndpointIds);

         Transaction.Current.OnCommittedSuccessfully(() => connections.ForEach(subscriberConnection =>
         {
            subscriberConnection.SendAsync(exactlyOnceEvent)
                                 //Bug: this returns a task that must be awaited somehow.
                                .ContinueAsynchronouslyOnDefaultScheduler(task => HandleDeliveryTaskResults(task, subscriberConnection.EndpointInformation.Id, exactlyOnceEvent.MessageId));
         }));
      }
   }

   public void SendTransactionally(IExactlyOnceCommand exactlyOnceCommand)
   {
      Assert.State.NotNull(Transaction.Current);
      var connection = _transport.ConnectionToHandlerFor(exactlyOnceCommand);

      _storage.SaveMessage(exactlyOnceCommand, connection.EndpointInformation.Id);

      Transaction.Current.OnCommittedSuccessfully(() =>
      {
         connection.SendAsync(exactlyOnceCommand)
                    //Bug: this returns a task that must be awaited somehow.
                   .ContinueAsynchronouslyOnDefaultScheduler(task => HandleDeliveryTaskResults(task, connection.EndpointInformation.Id, exactlyOnceCommand.MessageId));
      });
   }

   void HandleDeliveryTaskResults(Task completedSendTask, EndpointId receiverId, Guid messageId)
   {
      if(completedSendTask.IsFaulted)
      {
         //Todo: Handle delivery failures sanely.
      } else
      {
         _storage.MarkAsReceived(messageId, receiverId);
      }
   }

   public async Task StartAsync()
   {
      if(!_configuration.IsPureClientEndpoint)
         await _storage.StartAsync().CaF();
   }
}