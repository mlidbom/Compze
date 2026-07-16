using System.Transactions;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Implementation;
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
      //Wiring the outbox is what wires the endpoint's exactly-once tevent delivery: the outbox joins the delivery-leg set the IUnitOfWorkTeventPublisher routes through...
      registrar.Register(Singleton.ForSet<IExactlyOnceTeventDeliveryLeg>().CreatedBy((IOutbox outbox) => outbox));
      //...and what grants the router's connections their exactly-once delivery streams, backed by the outbox's storage: on an endpoint without the outbox this set is empty and connections carry no such stream (see TessagingConnection).
      registrar.Register(Singleton.ForSet<TessagingConnection.ExactlyOnceDeliveryStream.Factory>().CreatedBy((ITessageStorage tessageStorage) => new TessagingConnection.ExactlyOnceDeliveryStream.Factory(tessageStorage)));
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

   public void PublishTransactionally(IPublisherTevent<IExactlyOnceTevent> wrappedTevent)
   {
      State.NotNull(Transaction.Current);
      var dedupId = wrappedTevent.Tevent.Id;
      this.Log().Debug($"Will publish if transaction tevent if transactions succeeds: {dedupId} ({wrappedTevent.GetType().Name})");
      var connections = _tessagingRouter.SubscriberConnectionsFor(wrappedTevent)
                                        .Where(connection => connection.EndpointInformation.Id != _configuration.Id)
                                        .ToArray(); //We dispatch tevents to ourselves synchronously so don't go doing it again here.

      var teventHandlerEndpointIds = connections.Select(connection => connection.EndpointInformation.Id).ToArray();
      _storage.SaveTessage(wrappedTevent, dedupId, teventHandlerEndpointIds);

      Transaction.Current.OnCommittedSuccessfully(() => connections.ForEach(connection =>
      {
         this.Log().Debug($"OnCommittedSuccessfully: Delivering tevent {dedupId} to endpoint {connection.EndpointInformation.Id}");
         connection.EnqueueForExactlyOnceDelivery(wrappedTevent, dedupId);
      }));
   }

   public void SendTransactionally(IExactlyOnceTommand exactlyOnceTommand)
   {
      State.NotNull(Transaction.Current);
      this.Log().Debug($"Will send tommand if transactions succeeds: {exactlyOnceTommand.Id} ({exactlyOnceTommand.GetType().Name})");
      var connection = _tessagingRouter.ConnectionToHandlerFor(exactlyOnceTommand);

      _storage.SaveTessage(exactlyOnceTommand, exactlyOnceTommand.Id, connection.EndpointInformation.Id);

      Transaction.Current.OnCommittedSuccessfully(() =>
      {
         this.Log().Debug($"OnCommittedSuccessfully: Delivering tommand {exactlyOnceTommand.Id} to endpoint {connection.EndpointInformation.Id}");
         connection.EnqueueForExactlyOnceDelivery(exactlyOnceTommand, exactlyOnceTommand.Id);
      });
   }

   bool _running = false;

   //The router's delivery lifecycle is not the outbox's to drive: it belongs to the transient Tessaging core's component
   //(TransientTessagingEndpointComponent), which starts delivery only after every endpoint's listening phase — by which time
   //this storage is initialized, so each connection's exactly-once stream can load its recovery backlog.
   public async Task StartAsync()
   {
      State.Assert(!_running);
      this.Log().Info("Starting");

      await _storage.StartAsync().caf();

      _running = true;
      this.Log().Info("Started");
   }

   public async Task StopAsync()
   {
      State.Assert(_running);
      _running = false;
      this.Log().Info("Stopped");
      await Task.CompletedTask.caf();
   }
}
