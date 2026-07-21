using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Engine._private;
using Compze.Tessaging._internal.TessagesInFlight;
using Compze.Tessaging._private.SystemCE.ThreadingCE;
using Compze.Tessaging._internal.SqlLayer;
using Compze.Tessaging.TessageBus._internal;
using Compze.Tessaging.TessageTypes;
using Compze.TypeIdentifiers;
using JetBrains.Annotations;
using Compze.Tessaging._private.Transport;

namespace Compze.Tessaging.TessageBus._private.Inbox;

static class InboxRegistrar
{
   public static IComponentRegistrar Inbox(this IComponentRegistrar registrar)
      => registrar.Register(_private.Inbox.Inbox.RegisterWith);
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
                  .CreatedBy((HandlerExecutionEngine handlerExecutionEngine, ITessageStorage tessageStorage, TeventObservationDispatcher observationDispatcher, ITypeMap typeMap, ITessagingSerializer serializer)
                                => new Inbox(handlerExecutionEngine, tessageStorage, observationDispatcher, typeMap, serializer))
      );

   readonly HandlerExecutionEngine _handlerExecutionEngine;

   readonly ITessageStorage _storage;

   readonly TeventObservationDispatcher _observationDispatcher;
   readonly ITypeMap _typeMap;
   readonly ITessagingSerializer _serializer;

   internal Inbox(HandlerExecutionEngine handlerExecutionEngine, ITessageStorage tessageStorage, TeventObservationDispatcher observationDispatcher, ITypeMap typeMap, ITessagingSerializer serializer)
   {
      _handlerExecutionEngine = handlerExecutionEngine;
      _storage = tessageStorage;
      _observationDispatcher = observationDispatcher;
      _typeMap = typeMap;
      _serializer = serializer;
   }

   public async Task StartAsync()
   {
      this.Log().Info("Starting");
      _handlerExecutionEngine.Start();
      await _storage.StartAsync().caf();
      await ReEnqueueUnHandledTessagesAsync().caf();
      this.Log().Info("Started");
   }

   //The recovery scan: a hard crash between a tessage's admission and its handler-commit leaves the row UnHandled with no
   //redelivery coming - the sender was acknowledged at admission - so re-enqueueing such rows at start is what makes the
   //acknowledged-means-will-be-handled contract hold across crashes. Runs in the endpoint's listening phase, before the
   //transport server starts serving, so the backlog enters the engine ahead of anything newly arriving and per-pair handling
   //order survives the crash; the handling claim makes any double-enqueue this could ever race into a clean skip.
   //Observation is deliberately not re-dispatched: a recovered tessage was already registered once, and the dedup that
   //shields observers from redeliveries shields them from its re-registration too.
   async Task ReEnqueueUnHandledTessagesAsync()
   {
      var unHandled = await _storage.GetUnHandledTessagesAsync().caf();
      if(unHandled.Count == 0) return;

      this.Log().Info($"Recovery scan: re-enqueueing {unHandled.Count} admitted but unhandled tessage(s) for handling.");
      foreach(var tessage in unHandled)
         _handlerExecutionEngine.Enqueue(new TransportTessage.InComing(tessage.SerializedTessage, tessage.TypeId.CanonicalString, tessage.TessageId, _typeMap, _serializer));
   }

   public async Task ReceiveAsync(TransportTessage.InComing tessage)
   {
      this.Log().Debug($"Receiving {tessage.TessageTypeEnum} tessage {tessage.TessageId}");
      var saveResult = await _storage.SaveIncomingTessageAsync(tessage).caf();

      switch(saveResult)
      {
         case ITessagingSqlLayer.SaveTessageResult.Duplicate:
            //The dedup shields observers too: observation is dispatched only on a tessage's first registration, never for a redelivery.
            this.Log().Debug($"Skipping duplicate tessage {tessage.TessageId}");
            return;
         case ITessagingSqlLayer.SaveTessageResult.RefusedAwaitingItsPredecessor:
            //The refusal travels back over the transport as this delivery's failure; the sender's sequence-ordered retry
            //redelivers the stream in order, predecessor first.
            throw new TessageRefusedAwaitingItsPredecessorException(tessage);
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
