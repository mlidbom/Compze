using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
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
                  .CreatedBy((IServiceBusSqlLayer.IInboxSqlLayer sqlLayer)
                                => new InboxTessageStorage(sqlLayer)),
         Singleton.For<HandlerExecutionEngine>()
                  .CreatedBy((ITessagesInFlightTracker globalStateTracker, ITessageHandlerRegistry tessagingHandlerRegistry, IScopeFactory scopeFactory, ITessageStorage storage, ITaskRunner taskRunner, EndpointConfiguration configuration)
                                => new HandlerExecutionEngine(globalStateTracker, tessagingHandlerRegistry, scopeFactory, storage, taskRunner, configuration.Id)),
         Singleton.For<IInbox>()
                  .CreatedBy((HandlerExecutionEngine handlerExecutionEngine, ITessageStorage tessageStorage, ILegacyContainer container, IInboxTransportServer transportServer)
                                => new Inbox(handlerExecutionEngine, tessageStorage, container, transportServer))
      );

   readonly HandlerExecutionEngine _handlerExecutionEngine;

   readonly ITessageStorage _storage;
   readonly IInboxTransportServer _transportServer;

   public Inbox(HandlerExecutionEngine handlerExecutionEngine, ITessageStorage tessageStorage, ILegacyContainer container, IInboxTransportServer transportServer)
   {
      _handlerExecutionEngine = handlerExecutionEngine;
      _storage = tessageStorage;
      _transportServer = transportServer;
   }

   public EndPointAddress Address => new(uri: _transportServer.Address);

   public async Task StartAsync()
   {
      this.Log().Info("Starting");
      _handlerExecutionEngine.Start();
      var storageStartTask = _storage.StartAsync();
      await Task.WhenAll(storageStartTask, _transportServer.StartAsync()).caf();
      this.Log().Info($"Started at {Address}");
   }

   public Task ReceiveAsync(TransportTessage.InComing tessage)
   {
      this.Log().Debug($"Receiving {tessage.TessageTypeEnum} tessage {tessage.TessageId}");
      var saveResult = _storage.SaveIncomingTessage(tessage);

      if(saveResult == IServiceBusSqlLayer.SaveTessageResult.Duplicate)
      {
         this.Log().Debug($"Skipping duplicate tessage {tessage.TessageId}");
         return Task.CompletedTask;
      }

      _handlerExecutionEngine.Enqueue(tessage);
      return Task.CompletedTask;
   }

   public async Task StopAsync()
   {
      this.Log().Info("Stopping");
      await _transportServer.StopAsync().caf();
   }

   public async ValueTask DisposeAsync()
   {
      this.Log().Debug("Disposing");
      _handlerExecutionEngine.Stop();
      await StopAsync().caf();
      await _transportServer.DisposeAsync().caf();
   }
}
