using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Compze.Core.Public;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Public;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Contracts;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE.TransactionsCE;

namespace Compze.Tessaging.Implementation.Outbox;

public static class OutboxRegistrar
{
   public static IComponentRegistrar Outbox(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.Outbox.Outbox.RegisterWith);
}

#pragma warning disable CA1724 // Type name intentionally matches namespace concept
public partial class Outbox : IOutbox
{
   public static void RegisterWith(IComponentRegistrar registrar)
   {
      registrar.Register(Singleton.For<IOutbox>()
                                  .CreatedBy((EndpointConfiguration configuration, ITessagingRouter tessagingRouter, ITessageStorage tessageStorage, IBackgroundExceptionReporter exceptionReporter, OutboxRetryPoller retryPoller)
                                                => new Outbox(tessagingRouter, tessageStorage, configuration, exceptionReporter, retryPoller)));
      registrar.Register(TessageStorage.RegisterWith);
      registrar.Register(OutboxRetryPoller.RegisterWith);
   }

   readonly ITessageStorage _storage;
   readonly EndpointConfiguration _configuration;
   readonly ITessagingRouter _tessagingRouter;
   readonly IBackgroundExceptionReporter _exceptionReporter;
   readonly OutboxRetryPoller _retryPoller;

   Outbox(ITessagingRouter tessagingRouter, ITessageStorage tessageStorage, EndpointConfiguration configuration, IBackgroundExceptionReporter exceptionReporter, OutboxRetryPoller retryPoller)
   {
      _storage = tessageStorage;
      _configuration = configuration;
      _tessagingRouter = tessagingRouter;
      _exceptionReporter = exceptionReporter;
      _retryPoller = retryPoller;
   }

   public void PublishTransactionally(IExactlyOnceTevent exactlyOnceTevent)
   {
      Contract.State.NotNull(Transaction.Current);
      this.Log().Debug($"Outbox publishing tevent {exactlyOnceTevent.Id} ({exactlyOnceTevent.GetType().Name})");
      var connections = _tessagingRouter.SubscriberConnectionsFor(exactlyOnceTevent)
                                  .Where(connection => connection.EndpointInformation.Id != _configuration.Id)
                                  .ToArray(); //We dispatch tevents to ourselves synchronously so don't go doing it again here.;


      var teventHandlerEndpointIds = connections.Select(connection => connection.EndpointInformation.Id).ToArray();
      _storage.SaveTessage(exactlyOnceTevent, teventHandlerEndpointIds);

      Transaction.Current.OnCommittedSuccessfully(() => connections.ForEach(subscriberConnection =>
      {
         subscriberConnection.SendAsync(exactlyOnceTevent)
                             .ContinueWithCE(task => HandleDeliveryTaskResults(task, subscriberConnection.EndpointInformation.Id, exactlyOnceTevent.Id));
      }));
   }

   public void SendTransactionally(IExactlyOnceTommand exactlyOnceTommand)
   {
      Contract.State.NotNull(Transaction.Current);
      this.Log().Debug($"Outbox sending tommand {exactlyOnceTommand.Id} ({exactlyOnceTommand.GetType().Name})");
      var connection = _tessagingRouter.ConnectionToHandlerFor(exactlyOnceTommand);

      _storage.SaveTessage(exactlyOnceTommand, connection.EndpointInformation.Id);

      Transaction.Current.OnCommittedSuccessfully(() =>
      {
         connection.SendAsync(exactlyOnceTommand)
                   .ContinueWithCE(task => HandleDeliveryTaskResults(task, connection.EndpointInformation.Id, exactlyOnceTommand.Id));
      });
   }

   void HandleDeliveryTaskResults(Task completedSendTask, EndpointId receiverId, TessageId tessageId)
   {
      _exceptionReporter.RunSwallowingAndReportingAnyExceptions(() =>
      {
         if(!_running)
         {
            this.Log().Debug($"Delivery result for tessage {tessageId} ignored — outbox has stopped");
            return; //We have shut down and storage may no longer be available/working. The recovery mechanisms will take care of this tessage after restart.
         }
         if(completedSendTask.IsFaulted)
         {
            this.Log().Warning(completedSendTask.Exception!, $"Initial delivery failed for tessage {tessageId} to endpoint {receiverId}");
            _storage.RecordDeliveryFailure(tessageId, receiverId, completedSendTask.Exception);
         } else
         {
            this.Log().Debug($"Tessage {tessageId} delivered to endpoint {receiverId}");
            _storage.MarkAsReceived(tessageId, receiverId);
         }
      });
   }

   bool _running = false;
   public async Task StartAsync()
   {
      Contract.State.Assert(!_running);
      this.Log().Info("Outbox starting");

      await _storage.StartAsync().caf();
      _retryPoller.Start();

      _running = true;
      this.Log().Info("Outbox started");
   }

   public async Task StopAsync()
   {
      Contract.State.Assert(_running);
      this.Log().Info("Outbox stopping");
      _running = false;
      _retryPoller.Stop();
      this.Log().Info("Outbox stopped");
      await Task.CompletedTask.caf();
   }
}
