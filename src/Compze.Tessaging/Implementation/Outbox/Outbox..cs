using System.Transactions;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Peers;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
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
                                  .CreatedBy((ITessagingRouter tessagingRouter, ITessageStorage tessageStorage, IPeerRegistry peerRegistry)
                                                => new Outbox(tessagingRouter, tessageStorage, peerRegistry)));
      //Wiring the outbox is what wires the endpoint's exactly-once tevent delivery: the outbox joins the delivery-leg set the IUnitOfWorkTeventPublisher routes through...
      registrar.Register(Singleton.ForSet<IExactlyOnceTeventDeliveryLeg>().CreatedBy((IOutbox outbox) => outbox));
      //...and what grants the router's connections their exactly-once delivery streams, backed by the outbox's storage: on an endpoint without the outbox this set is empty and connections carry no such stream (see TessagingConnection).
      registrar.Register(Singleton.ForSet<TessagingConnection.ExactlyOnceDeliveryStream.Factory>().CreatedBy((ITessageStorage tessageStorage) => new TessagingConnection.ExactlyOnceDeliveryStream.Factory(tessageStorage)));
      //...and the outbox's side of the peer lifecycle: the peer registry notifies the observer set on every recorded advertisement, and this member keeps the undelivered rows consistent with what each peer's advertisement declares (shrink pruning, first-contact sweep); as the decommission participant it discards everything the outbox still owes a decommissioned peer. An endpoint without the outbox contributes neither.
      registrar.Register(Singleton.ForSet<IPeerLifecycleObserver>().CreatedBy((ITessageStorage tessageStorage) => new PeerLifecycleObserver(tessageStorage)));
      registrar.Register(Singleton.ForSet<IPeerDecommissionParticipant>().CreatedBy((ITessageStorage tessageStorage) => new PeerLifecycleObserver(tessageStorage)));
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

      Transaction.Current.OnCommittedSuccessfully(() =>
      {
         //Delivery starts now toward the listed peers' live connections, looked up at commit rather than at publish: a
         //subscriber connection that appeared in between loaded its recovery backlog before this row committed, so only a
         //commit-time lookup sees it. Intersecting keeps enqueue ⊆ persist (the registry is written before routes are built
         //from an advertisement — see TessagingRouter.ConnectAsync); a listed peer with no live connection here is exactly
         //what the backlog load covers when its next connection's delivery starts.
         _tessagingRouter.SubscriberConnectionsFor(wrappedTevent)
                         .Where(connection => subscriberIds.Contains(connection.EndpointInformation.Id))
                         .ForEach(connection =>
                          {
                             this.Log().Debug($"OnCommittedSuccessfully: Delivering tevent {dedupId} to endpoint {connection.EndpointInformation.Id}");
                             connection.EnqueueForExactlyOnceDelivery(wrappedTevent, dedupId);
                          });
      });
   }

   public void SendTransactionally(IExactlyOnceTommand exactlyOnceTommand)
   {
      State.NotNull(Transaction.Current);
      this.Log().Debug($"Will send tommand if transaction succeeds: {exactlyOnceTommand.Id} ({exactlyOnceTommand.GetType().Name})");

      //The tommand binds to its one specific receiver here, at send: every tessage between a sender and a receiver rides that
      //pair's single ordered, receiver-deduped delivery stream, which is what makes exactly-once in-order hold by construction.
      //(Routing at delivery time was tried and retracted: re-delivery could reach an endpoint whose inbox never saw the
      //tommand, breaking exactly-once across handler replacement - see dev_docs/TODO/durable-peer-topology.md.)
      var receiverId = ResolveReceiver(exactlyOnceTommand);
      _storage.SaveTessage(exactlyOnceTommand, exactlyOnceTommand.Id, receiverId);

      Transaction.Current.OnCommittedSuccessfully(() =>
      {
         //Looked up at commit rather than at send: a connection that appeared in between loaded its recovery backlog before
         //this row committed, so only a commit-time lookup sees it. The row is bound: it must enter no other endpoint's
         //stream, so a live handler that is not the bound receiver leaves the row waiting for its endpoint's return.
         var liveConnection = _tessagingRouter.LiveConnectionToHandlerFor(exactlyOnceTommand);
         if(liveConnection == null || !liveConnection.EndpointInformation.Id.Equals(receiverId)) return;
         this.Log().Debug($"OnCommittedSuccessfully: Delivering tommand {exactlyOnceTommand.Id} to endpoint {receiverId}");
         liveConnection.EnqueueForExactlyOnceDelivery(exactlyOnceTommand, exactlyOnceTommand.Id);
      });
   }

   ///<summary>The one endpoint this send binds to: the live handler when one is connected — current by definition, and the only<br/>
   /// route that can name the endpoint itself, which the registry never lists — otherwise the sole remembered peer whose<br/>
   /// advertisement handles the type. No remembered handler fails loud (<see cref="NoHandlerForTessageTypeException"/>); more<br/>
   /// than one — a handler replacement whose retired peer was never decommissioned, with none of them up — fails loud too<br/>
   /// (<see cref="MultipleHandlersForTessageTypeException"/>), because binding to the wrong one would strand the tommand.</summary>
   EndpointId ResolveReceiver(IExactlyOnceTommand exactlyOnceTommand)
   {
      var liveConnection = _tessagingRouter.LiveConnectionToHandlerFor(exactlyOnceTommand);
      if(liveConnection != null) return liveConnection.EndpointInformation.Id;

      var rememberedHandlerIds = _peerRegistry.HandlerIdsFor(exactlyOnceTommand);
      return rememberedHandlerIds.Count switch
      {
         0 => throw new NoHandlerForTessageTypeException(exactlyOnceTommand.GetType()),
         1 => rememberedHandlerIds[0],
         _ => throw new MultipleHandlersForTessageTypeException(exactlyOnceTommand.GetType(), rememberedHandlerIds)
      };
   }

   bool _running = false;

   //The router's delivery lifecycle is not the outbox's to drive: it belongs to the distributed Tessaging core's component
   //(DistributedTessagingEndpointComponent), which starts delivery only after every endpoint's listening phase — by which time
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
