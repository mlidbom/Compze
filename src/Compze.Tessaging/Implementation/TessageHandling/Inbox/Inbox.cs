using Compze.Abstractions.Hosting.Public;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Transport.SqlLayer;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation.TessageHandling.Inbox;

static class InboxRegistrar
{
   public static IComponentRegistrar Inbox(this IComponentRegistrar registrar)
      => registrar.Register(TessageHandling.Inbox.Inbox.RegisterWith);
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
                  .CreatedBy((ITessagesInFlightTracker globalStateTracker, ITessageHandlerRegistry tessagingHandlerRegistry, IScopeFactory scopeFactory, ITessageStorage storage, ITaskRunner taskRunner, EndpointConfiguration configuration)
                                => new HandlerExecutionEngine(globalStateTracker, tessagingHandlerRegistry, scopeFactory, storage, taskRunner, configuration.Id)),
         Singleton.For<IInbox>()
                  .CreatedBy((HandlerExecutionEngine handlerExecutionEngine, ITessageStorage tessageStorage, TeventObservationDispatcher teventObservationDispatcher)
                                => new Inbox(handlerExecutionEngine, tessageStorage, teventObservationDispatcher))
      );

   readonly HandlerExecutionEngine _handlerExecutionEngine;

   readonly ITessageStorage _storage;

   readonly TeventObservationDispatcher _teventObservationDispatcher;

   internal Inbox(HandlerExecutionEngine handlerExecutionEngine, ITessageStorage tessageStorage, TeventObservationDispatcher teventObservationDispatcher)
   {
      _handlerExecutionEngine = handlerExecutionEngine;
      _storage = tessageStorage;
      _teventObservationDispatcher = teventObservationDispatcher;
   }

   public async Task StartAsync()
   {
      this.Log().Info("Starting");
      _handlerExecutionEngine.Start();
      await _storage.StartAsync().caf();
      this.Log().Info("Started");
   }

   public Task ReceiveAsync(TransportTessage.InComing tessage)
   {
      this.Log().Debug($"Receiving {tessage.TessageTypeEnum} tessage {tessage.TessageId}");
      var saveResult = _storage.SaveIncomingTessage(tessage);

      if(saveResult == ITessagingSqlLayer.SaveTessageResult.Duplicate)
      {
         //The dedup shields observers too: observation is dispatched only on a tessage's first registration, never for a redelivery.
         this.Log().Debug($"Skipping duplicate tessage {tessage.TessageId}");
         return Task.CompletedTask;
      }

      //Observation fires at registration: after dedup, before the transactional processing the engine schedules.
      if(tessage.TessageTypeEnum == TransportTessageType.ExactlyOnceTevent)
         _teventObservationDispatcher.Dispatch(tessage);

      _handlerExecutionEngine.Enqueue(tessage);
      return Task.CompletedTask;
   }

   public ValueTask DisposeAsync()
   {
      this.Log().Debug("Disposing");
      _handlerExecutionEngine.Stop();
      return ValueTask.CompletedTask;
   }
}
