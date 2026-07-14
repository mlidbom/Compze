using System.Transactions;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;

namespace Compze.Tessaging.Implementation.Outbox;

static class OutboxRegistrar
{
   public static IComponentRegistrar Outbox(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.Outbox.Outbox.RegisterWith);
}

#pragma warning disable CA1724 // Type name intentionally matches namespace concept
partial class Outbox : IOutbox
{
   internal static void RegisterWith(IComponentRegistrar registrar)
   {
      registrar.Register(Singleton.For<IOutbox>()
                                  .CreatedBy((EndpointConfiguration configuration, ITessagingRouter tessagingRouter, ITessageStorage tessageStorage)
                                                => new Outbox(tessagingRouter, tessageStorage, configuration)));
      //Wiring the outbox is what wires the endpoint's exactly-once tevent delivery: the outbox joins the delivery-leg set the ITeventPublisher routes through.
      registrar.Register(Singleton.ForSet<IExactlyOnceTeventDeliveryLeg>().CreatedBy((IOutbox outbox) => outbox));
      registrar.Register(TessageStorage.RegisterWith);
   }

   readonly ITessageStorage _storage;
   readonly EndpointConfiguration _configuration;
   readonly ITessagingRouter _tessagingRouter;

   Outbox(ITessagingRouter tessagingRouter, ITessageStorage tessageStorage, EndpointConfiguration configuration)
   {
      _storage = tessageStorage;
      _configuration = configuration;
      _tessagingRouter = tessagingRouter;
   }

   public void PublishTransactionally(IPublisherIdentifyingTevent<IExactlyOnceTevent> wrappedTevent)
   {
      State.NotNull(Transaction.Current);
      var dedupId = wrappedTevent.Tevent.Id;
      this.Log().Debug($"Publishing tevent {dedupId} ({wrappedTevent.GetType().Name})");
      var connections = _tessagingRouter.SubscriberConnectionsFor(wrappedTevent)
                                        .Where(connection => connection.EndpointInformation.Id != _configuration.Id)
                                        .ToArray(); //We dispatch tevents to ourselves synchronously so don't go doing it again here.

      var teventHandlerEndpointIds = connections.Select(connection => connection.EndpointInformation.Id).ToArray();
      _storage.SaveTessage(wrappedTevent, dedupId, teventHandlerEndpointIds);

      Transaction.Current.OnCommittedSuccessfully(() => connections.ForEach(connection =>
      {
         this.Log().Debug($"OnCommittedSuccessfully: Delivering tevent {dedupId} to endpoint {connection.EndpointInformation.Id}");
         connection.EnqueueForDelivery(wrappedTevent, dedupId);
      }));
   }

   public void SendTransactionally(IExactlyOnceTommand exactlyOnceTommand)
   {
      State.NotNull(Transaction.Current);
      this.Log().Debug($"Sending tommand {exactlyOnceTommand.Id} ({exactlyOnceTommand.GetType().Name})");
      var connection = _tessagingRouter.ConnectionToHandlerFor(exactlyOnceTommand);

      _storage.SaveTessage(exactlyOnceTommand, exactlyOnceTommand.Id, connection.EndpointInformation.Id);

      Transaction.Current.OnCommittedSuccessfully(() =>
      {
         this.Log().Debug($"OnCommittedSuccessfully: Delivering tommand {exactlyOnceTommand.Id} to endpoint {connection.EndpointInformation.Id}");
         connection.EnqueueForDelivery(exactlyOnceTommand, exactlyOnceTommand.Id);
      });
   }

   bool _running = false;

   public async Task StartAsync()
   {
      State.Assert(!_running);
      this.Log().Info("Starting");

      await _storage.StartAsync().caf();

      _tessagingRouter.StartDelivery();

      _running = true;
      this.Log().Info("Started");
   }

   public async Task StopAsync()
   {
      State.Assert(_running);
      this.Log().Info("Stopping");
      _running = false;

      _tessagingRouter.StopDelivery();

      this.Log().Info("Stopped");
      await Task.CompletedTask.caf();
   }
}
