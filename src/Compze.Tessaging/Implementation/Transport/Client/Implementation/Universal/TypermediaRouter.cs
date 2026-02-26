using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.Implementation.Transport.Client.Routing;
using Compze.Contracts;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Functional;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;

public static class TransportRegistrar
{
   public static IComponentRegistrar Transport(this IComponentRegistrar registrar)
      => registrar.Register(TypermediaRouter.RegisterWith)
                  .Register(TessagingRouter.RegisterWith);
}

public class TypermediaRouter : ITypermediaRouter, IDisposable
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(
            Singleton.For<InboxConnectionRouter>().CreatedBy((ITypeMapper typeMapper) => new InboxConnectionRouter(typeMapper)),
            Singleton.For<ITypermediaRouter>().CreatedBy(
               (InboxConnectionRouter router, ITessagesInFlightTracker tessagesInFlightTracker, ITypeMapper typeMapper, IRemotableTessageSerializer serializer, ITransportMessagePoster transportMessagePoster)
                  => new TypermediaRouter(router, tessagesInFlightTracker, typeMapper, serializer, transportMessagePoster)));

   TypermediaRouter(InboxConnectionRouter inboxConnectionRouter, ITessagesInFlightTracker tessagesInFlightTracker, ITypeMapper typeMapper, IRemotableTessageSerializer serializer, ITransportMessagePoster transportMessagePoster)
   {
      _inboxConnectionRouter = inboxConnectionRouter;
      _tessagesInFlightTracker = tessagesInFlightTracker;
      _typeMapper = typeMapper;
      _serializer = serializer;
      _transportMessagePoster = transportMessagePoster;
   }

   readonly InboxConnectionRouter _inboxConnectionRouter;
   readonly ITessagesInFlightTracker _tessagesInFlightTracker;
   readonly ITypeMapper _typeMapper;
   readonly IRemotableTessageSerializer _serializer;
   readonly ITransportMessagePoster _transportMessagePoster;

   bool _running = false;
   IReadOnlyDictionary<EndpointId, RemoteEndpointConnection> _connections = new Dictionary<EndpointId, RemoteEndpointConnection>();

   public async Task ConnectAsync(EndPointAddress remoteEndpointAddress)
   {
      AssertRunning();
#pragma warning disable CA2000//We are passing this disposable into a collection that we track disposal for
      var connection = new RemoteEndpointConnection(_tessagesInFlightTracker, remoteEndpointAddress, _typeMapper, _serializer, _transportMessagePoster);
#pragma warning restore CA2000

      await connection.InitAsync().caf();

      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _connections, connection.EndpointInformation.Id, connection);

      //urgent: we can't have routes be discovered at startup based on the assumption that all endpoints are up...
      _inboxConnectionRouter.RegisterRoutes(connection, connection.EndpointInformation.HandledTessageTypes);
   }

   public async Task PostAsync(IAtMostOnceTypermediaTommand tommand)
   {
      AssertRunning();
      var connection = _inboxConnectionRouter.ConnectionToHandlerFor(tommand);
      await connection.ApiClient.PostAsync(tommand).caf();
   }

   public async Task<TTommandResult> PostAsync<TTommandResult>(IAtMostOnceTommand<TTommandResult> typermediaTommand)
   {
      AssertRunning();
      var connection = _inboxConnectionRouter.ConnectionToHandlerFor(typermediaTommand);
      return await connection.ApiClient.PostAsync(typermediaTommand).caf();
   }

   public async Task<TTueryResult> GetAsync<TTueryResult>(IRemotableTuery<TTueryResult> tuery)
   {
      AssertRunning();
      var connection = _inboxConnectionRouter.ConnectionToHandlerFor(tuery);
      return await connection.ApiClient.GetAsync(tuery).caf();
   }

   public void Start() => Contract.State.Assert(!_running, () => "already running")
                                ._then(_running = true);

   public void Stop() => AssertRunning()._then(() =>
   {
      _running = false;
      _inboxConnectionRouter.Stop();
   });

   bool _disposed;

   public void Dispose()
   {
      if(!_disposed)
      {
         _disposed = true;
         if(_running)
         {
            Stop();
         }

         _connections.Values.DisposeAll();
      }
   }

   unit AssertRunning() => Contract.State.Assert(_running, () => "not running")._then(unit.Value);
}
