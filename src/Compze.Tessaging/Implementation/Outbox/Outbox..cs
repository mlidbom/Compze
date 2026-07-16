using System.Transactions;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Peers;
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
                                  .CreatedBy((ITessagingRouter tessagingRouter, ITessageStorage tessageStorage, IComponentSet<IPeerRegistry> peerRegistry)
                                                => new Outbox(tessagingRouter, tessageStorage, peerRegistry.Single())));
      //Wiring the outbox is what wires the endpoint's exactly-once tevent delivery: the outbox joins the delivery-leg set the IUnitOfWorkTeventPublisher routes through...
      registrar.Register(Singleton.ForSet<IExactlyOnceTeventDeliveryLeg>().CreatedBy((IOutbox outbox) => outbox));
      //...and what grants the router's connections their exactly-once delivery streams, backed by the outbox's storage: on an endpoint without the outbox this set is empty and connections carry no such stream (see TessagingConnection).
      registrar.Register(Singleton.ForSet<TessagingConnection.ExactlyOnceDeliveryStream.Factory>().CreatedBy((ITessageStorage tessageStorage) => new TessagingConnection.ExactlyOnceDeliveryStream.Factory(tessageStorage)));
      registrar.Register(TessageStorage.RegisterWith);
   }

   readonly ITessageStorage _storage;
   readonly IPeerRegistry _peerRegistry;
   readonly ITessagingRouter _tessagingRouter;

   Outbox(ITessagingRouter tessagingRouter, ITessageStorage tessageStorage, IPeerRegistry peerRegistry)
   {
      _storage = tessageStorage;
      _peerRegistry = peerRegistry;
      _tessagingRouter = tessagingRouter;
   }

   public void PublishTransactionally(IPublisherTevent<IExactlyOnceTevent> wrappedTevent)
   {
      State.NotNull(Transaction.Current);
      var dedupId = wrappedTevent.Tevent.Id;
      this.Log().Debug($"Will publish tevent if transaction succeeds: {dedupId} ({wrappedTevent.GetType().Name})");

      //Fan-out membership is the peer registry's remembered subscribers, never the live connections: a subscribing peer that is
      //down at publish time still gets its receiver row, and the recovery backlog its next connection loads delivers the tevent
      //on its return. (A peer is another endpoint, so the registry never lists us: tevents to ourselves dispatch synchronously in-process.)
      var subscriberIds = _peerRegistry.SubscriberIdsFor(wrappedTevent);
      _storage.SaveTessage(wrappedTevent, dedupId, [..subscriberIds]);

      //Delivery starts now only toward the listed peers' live connections. Intersecting keeps enqueue ⊆ persist even while an
      //advertisement update is mid-connect (the registry is written before routes are built from it — see TessagingRouter.ConnectAsync),
      //and a peer persisted here but not enqueued is exactly what the backlog load covers when its connection's delivery starts.
      var connectionsToEnqueueOn = _tessagingRouter.SubscriberConnectionsFor(wrappedTevent)
                                                   .Where(connection => subscriberIds.Contains(connection.EndpointInformation.Id))
                                                   .ToArray();

      Transaction.Current.OnCommittedSuccessfully(() => connectionsToEnqueueOn.ForEach(connection =>
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
