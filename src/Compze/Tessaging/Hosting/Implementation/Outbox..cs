using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Compze.Utilities.SystemCE.TransactionsCE;

namespace Compze.Tessaging.Hosting.Implementation;

static class OutboxRegistrar
{
   internal static IDependencyRegistrar Outbox(this IDependencyRegistrar registrar)
      => registrar.Register(Implementation.Outbox.RegisterWith);
}

partial class Outbox : IOutbox
{
   internal static void RegisterWith(IDependencyRegistrar registrar)
   {
      registrar.Register(Singleton.For<IOutbox>()
                                  .CreatedBy((EndpointConfiguration configuration, ITransport transport, Outbox.IMessageStorage messageStorage)
                                                => new Outbox(transport, messageStorage, configuration)));
      registrar.Register(MessageStorage.RegisterWith);
   }

   readonly IMessageStorage _storage;
   readonly EndpointConfiguration _configuration;
   readonly ITransport _transport;

   Outbox(ITransport transport, Outbox.IMessageStorage messageStorage, EndpointConfiguration configuration)
   {
      _storage = messageStorage;
      _configuration = configuration;
      _transport = transport;
   }

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
            TaskCE.ContinueAsynchronouslyOnDefaultScheduler(subscriberConnection.SendAsync(exactlyOnceEvent), task => HandleDeliveryTaskResults(task, subscriberConnection.EndpointInformation.Id, exactlyOnceEvent.MessageId));
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
         await _storage.StartAsync().caf();
   }
}
