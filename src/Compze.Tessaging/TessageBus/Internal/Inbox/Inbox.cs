using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Engine.Internal;
using Compze.Tessaging.TessageBus.Internal.BestEffortDelivery;
using Compze.Tessaging.TessageBus.Internal.Outbox;
using Compze.Tessaging.Internal.TessagesInFlight;
using Compze.Tessaging.Internal.SystemCE.ThreadingCE;
using Compze.Tessaging.Internal.Transport;
using Compze.Tessaging.Internal.SqlLayer;
using JetBrains.Annotations;

namespace Compze.Tessaging.TessageBus.Internal.Inbox;

static class InboxRegistrar
{
   public static IComponentRegistrar Inbox(this IComponentRegistrar registrar)
      => registrar.Register(Internal.Inbox.Inbox.RegisterWith);
}

#pragma warning disable CA1724 // Type name intentionally matches namespace concept
[UsedImplicitly] partial class Inbox : IInbox, IAsyncDisposable
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(
         Singleton.For<ITessageStorage>()
                  .CreatedBy((ITessagingSqlLayer.IInboxSqlLayer sqlLayer)
                                => new InboxTessageStorage(sqlLayer)),
         Singleton.For<HandlerExecutionEngine>()
                  .CreatedBy((ITessagesInFlightTracker globalStateTracker, TessageHandlerExecutor executor, IScopeFactory scopeFactory, ITessageStorage storage, ITaskRunner taskRunner, EndpointConfiguration configuration)
                                => new HandlerExecutionEngine(globalStateTracker, executor, scopeFactory, storage, taskRunner, configuration.Id)),
         Singleton.For<IInbox>()
                  .CreatedBy((HandlerExecutionEngine handlerExecutionEngine, ITessageStorage tessageStorage, TeventObservationDispatcher observationDispatcher)
                                => new Inbox(handlerExecutionEngine, tessageStorage, observationDispatcher))
      );

   readonly HandlerExecutionEngine _handlerExecutionEngine;

   readonly ITessageStorage _storage;

   readonly TeventObservationDispatcher _observationDispatcher;

   internal Inbox(HandlerExecutionEngine handlerExecutionEngine, ITessageStorage tessageStorage, TeventObservationDispatcher observationDispatcher)
   {
      _handlerExecutionEngine = handlerExecutionEngine;
      _storage = tessageStorage;
      _observationDispatcher = observationDispatcher;
   }

   public async Task StartAsync()
   {
      this.Log().Info("Starting");
      _handlerExecutionEngine.Start();
      await _storage.StartAsync().caf();
      this.Log().Info("Started");
   }

   public async Task ReceiveAsync(TransportTessage.InComing tessage)
   {
      this.Log().Debug($"Receiving {tessage.TessageTypeEnum} tessage {tessage.TessageId}");
      var saveResult = await _storage.SaveIncomingTessageAsync(tessage).caf();

      if(saveResult == ITessagingSqlLayer.SaveTessageResult.Duplicate)
      {
         //The dedup shields observers too: observation is dispatched only on a tessage's first registration, never for a redelivery.
         this.Log().Debug($"Skipping duplicate tessage {tessage.TessageId}");
         return;
      }

      //Observation queues at registration: after dedup - so the dedup shields observers from redeliveries - and before the
      //transactional processing the engine schedules. The arriving tevent is already a committed fact on its publisher, so
      //committed-facts-only holds by construction; dispatch is off-thread, per-observer FIFO. Deserialization-frugal: the wrapper
      //type on the envelope answers whether observers match, so an arriving tevent nothing observes is never deserialized here.
      if(tessage.TessageTypeEnum == TransportTessageType.ExactlyOnceTevent && _observationDispatcher.AnyTeventObserversFor(tessage.TessageTypeId.Type))
         _observationDispatcher.QueueForObservers(PublisherTevent.Wrapped((ITevent)tessage.DeserializeTessageAndCacheForNextCall()));

      _handlerExecutionEngine.Enqueue(tessage);
   }

   public void AwaitAllReceivedTessagesProcessed() => _handlerExecutionEngine.AwaitAllReceivedTessagesProcessed();

   public ValueTask DisposeAsync()
   {
      this.Log().Debug("Disposing");
      _handlerExecutionEngine.Dispose();
      return ValueTask.CompletedTask;
   }
}
