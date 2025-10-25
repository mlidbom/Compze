using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Public;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.TransactionsCE;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Implementation.Outbox;

static class OutboxRegistrar
{
   internal static IComponentRegistrar Outbox(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.Outbox.Outbox.RegisterWith);
}

partial class Outbox : IOutbox
{
   internal static void RegisterWith(IComponentRegistrar registrar)
   {
      registrar.Register(Singleton.For<IOutbox>()
                                  .CreatedBy((EndpointConfiguration configuration, ITransportClient transportClient, ITessageStorage tessageStorage, IBackgroundExceptionReporter exceptionReporter, OutboxRetryPoller retryPoller)
                                                => new Outbox(transportClient, tessageStorage, configuration, exceptionReporter, retryPoller)));
      registrar.Register(TessageStorage.RegisterWith);
      registrar.Register(OutboxRetryPoller.RegisterWith);
   }

   readonly ITessageStorage _storage;
   readonly EndpointConfiguration _configuration;
   readonly ITransportClient _transportClient;
   readonly IBackgroundExceptionReporter _exceptionReporter;
   readonly OutboxRetryPoller _retryPoller;

   Outbox(ITransportClient transportClient, ITessageStorage tessageStorage, EndpointConfiguration configuration, IBackgroundExceptionReporter exceptionReporter, OutboxRetryPoller retryPoller)
   {
      _storage = tessageStorage;
      _configuration = configuration;
      _transportClient = transportClient;
      _exceptionReporter = exceptionReporter;
      _retryPoller = retryPoller;
   }

   public void PublishTransactionally(IExactlyOnceTevent exactlyOnceTevent)
   {
      Assert.State.NotNull(Transaction.Current);
      var connections = _transportClient.SubscriberConnectionsFor(exactlyOnceTevent)
                                  .Where(connection => connection.EndpointInformation.Id != _configuration.Id)
                                  .ToArray(); //We dispatch tevents to ourselves synchronously so don't go doing it again here.;

      //Urgent: bug. Our traceability thinking does not allow just discarding this tessage.But removing this if statement breaks a lot of tests that uses endpoint wiring but do not start an endpoint.
      if(connections.Length != 0)
      {
         var teventHandlerEndpointIds = connections.Select(connection => connection.EndpointInformation.Id).ToArray();
         _storage.SaveTessage(exactlyOnceTevent, teventHandlerEndpointIds);

         Transaction.Current.OnCommittedSuccessfully(() => connections.ForEach(subscriberConnection =>
         {
            subscriberConnection.SendAsync(exactlyOnceTevent)
                                .ContinueAsynchronouslyOnDefaultScheduler(task => HandleDeliveryTaskResults(task, subscriberConnection.EndpointInformation.Id, exactlyOnceTevent.TessageId));
         }));
      }
   }

   public void SendTransactionally(IExactlyOnceTommand exactlyOnceTommand)
   {
      Assert.State.NotNull(Transaction.Current);
      var connection = _transportClient.ConnectionToHandlerFor(exactlyOnceTommand);

      _storage.SaveTessage(exactlyOnceTommand, connection.EndpointInformation.Id);

      Transaction.Current.OnCommittedSuccessfully(() =>
      {
         connection.SendAsync(exactlyOnceTommand)
                   .ContinueAsynchronouslyOnDefaultScheduler(task => HandleDeliveryTaskResults(task, connection.EndpointInformation.Id, exactlyOnceTommand.TessageId));
      });
   }

   void HandleDeliveryTaskResults(Task completedSendTask, EndpointId receiverId, Guid tessageId)
   {
      _exceptionReporter.RunSwallowingAndReportingAnyExceptions(() =>
      {
         if(!_running)
            return; //We have shut down and storage may no longer be available/working. The recovery mechanisms will take care of this tessage after restart.
         if(completedSendTask.IsFaulted)
         {
            _storage.RecordDeliveryFailure(tessageId, receiverId, completedSendTask.Exception);
         } else
         {
            _storage.MarkAsReceived(tessageId, receiverId);
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
