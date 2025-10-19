using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.TransactionsCE;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Hosting.Implementation;

static class OutboxRegistrar
{
   internal static IComponentRegistrar Outbox(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.Outbox.RegisterWith);
}

partial class Outbox : IOutbox
{
   internal static void RegisterWith(IComponentRegistrar registrar)
   {
      registrar.Register(Singleton.For<IOutbox>()
                                  .CreatedBy((EndpointConfiguration configuration, ITransport transport, IMessageStorage messageStorage, IBackgroundExceptionReporter exceptionReporter, OutboxRetryPoller retryPoller)
                                                => new Outbox(transport, messageStorage, configuration, exceptionReporter, retryPoller)));
      registrar.Register(MessageStorage.RegisterWith);
      registrar.Register(OutboxRetryPoller.RegisterWith);
   }

   readonly IMessageStorage _storage;
   readonly EndpointConfiguration _configuration;
   readonly ITransport _transport;
   readonly IBackgroundExceptionReporter _exceptionReporter;
   readonly OutboxRetryPoller _retryPoller;

   Outbox(ITransport transport, IMessageStorage messageStorage, EndpointConfiguration configuration, IBackgroundExceptionReporter exceptionReporter, OutboxRetryPoller retryPoller)
   {
      _storage = messageStorage;
      _configuration = configuration;
      _transport = transport;
      _exceptionReporter = exceptionReporter;
      _retryPoller = retryPoller;
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
            subscriberConnection.SendAsync(exactlyOnceEvent)
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
                   .ContinueAsynchronouslyOnDefaultScheduler(task => HandleDeliveryTaskResults(task, connection.EndpointInformation.Id, exactlyOnceCommand.MessageId));
      });
   }

   void HandleDeliveryTaskResults(Task completedSendTask, EndpointId receiverId, Guid messageId)
   {
      _exceptionReporter.RunSwallowingAndReportingAnyExceptions(() =>
      {
         if(!_running)
            return; //We have shut down and storage may no longer be available/working. The recovery mechanisms will take care of this message after restart.
         if(completedSendTask.IsFaulted)
         {
            _storage.RecordDeliveryFailure(messageId, receiverId, completedSendTask.Exception);
         } else
         {
            _storage.MarkAsReceived(messageId, receiverId);
         }
      });
   }

   bool _running = false;

   public async Task StopAsync()
   {
      Assert.State.Is(_running);
      _running = false;
      _retryPoller.Stop();
      await Task.CompletedTask.caf();
   }

   public async Task StartAsync()
   {
      Assert.State.Is(!_running);

      if(!_configuration.IsPureClientEndpoint)
      {
         await _storage.StartAsync().caf();
         _retryPoller.Start();
      }

      _running = true;
   }
}
