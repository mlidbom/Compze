using System;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation.TessageHandling.Inbox;

public static class InboxRegistrar
{
   public static IComponentRegistrar Inbox(this IComponentRegistrar registrar)
      => registrar.Register(TessageHandling.Inbox.Inbox.RegisterWith);
}

#pragma warning disable CA1724 // Type name intentionally matches namespace concept
[UsedImplicitly] partial class Inbox : IInbox, IAsyncDisposable
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(
         Singleton.For<Inbox.ITessageStorage>()
                  .CreatedBy((IServiceBusSqlLayer.IInboxSqlLayer sqlLayer)
                                => new InboxTessageStorage(sqlLayer)),
         Singleton.For<Inbox.HandlerExecutionEngine>()
                  .CreatedBy((ITessagesInFlightTracker globalStateTracker, ITessageHandlerRegistry handlerRegistry, IServiceLocator serviceLocator, Inbox.ITessageStorage storage, ITaskRunner taskRunner, EndpointConfiguration configuration)
                                => new Inbox.HandlerExecutionEngine(globalStateTracker, handlerRegistry, serviceLocator, storage, taskRunner, configuration.Id)),
         Singleton.For<IInbox>()
                  .CreatedBy((IServiceLocator serviceLocator, Inbox.HandlerExecutionEngine handlerExecutionEngine, Inbox.ITessageStorage tessageStorage, IDependencyInjectionContainer container, IInboxTransportServer transportServer)
                                => new Inbox(serviceLocator, handlerExecutionEngine, tessageStorage, container, transportServer))
      );

   readonly HandlerExecutionEngine _handlerExecutionEngine;

   readonly ITessageStorage _storage;
   readonly IInboxTransportServer _transportServer;

   public Inbox(IServiceLocator serviceLocator, HandlerExecutionEngine handlerExecutionEngine, ITessageStorage tessageStorage, IDependencyInjectionContainer container, IInboxTransportServer transportServer)
   {
      _handlerExecutionEngine = handlerExecutionEngine;
      _storage = tessageStorage;
      _transportServer = transportServer;
   }

   public EndPointAddress Address => new(uri: _transportServer.Address);

   public async Task StartAsync()
   {
      _handlerExecutionEngine.Start();
      var storageStartTask = _storage.StartAsync();
      await Task.WhenAll(storageStartTask, _transportServer.StartAsync()).caf();
   }

   public Task ReceiveAsync(TransportTessage.InComing tessage)
   {
      var saveResult = _storage.SaveIncomingTessage(tessage);

      if(saveResult == IServiceBusSqlLayer.SaveTessageResult.Duplicate)
      {
         return Task.CompletedTask;
      }

      _handlerExecutionEngine.Enqueue(tessage);
      return Task.CompletedTask;
   }

   public async Task<object?> ExecuteAsync(TransportTessage.InComing tessage)
   {
      var saveResult = _storage.SaveIncomingTessage(tessage);

      if(saveResult == IServiceBusSqlLayer.SaveTessageResult.Duplicate)
      {
         return null;
      }

      return await _handlerExecutionEngine.ExecuteAsync(tessage).caf();
   }

   public async Task StopAsync() => await _transportServer.StopAsync().caf();

   public async ValueTask DisposeAsync()
   {
      _handlerExecutionEngine.Stop();
      await StopAsync().caf();
      await _transportServer.DisposeAsync().caf();
   }
}
