using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.Implementation.Transport.Client.Routing.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Contracts;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Typermedia.Hosting;

namespace Compze.Hosting;

class Endpoint : IEndpoint
{
   class ServerComponents(TommandScheduler tommandScheduler, IInbox inbox, IOutbox outbox) : IDisposable
   {
      internal readonly TommandScheduler TommandScheduler = tommandScheduler;
      internal readonly IInbox Inbox = inbox;
      internal readonly IOutbox Outbox = outbox;

      public void Dispose() => TommandScheduler.Dispose();
   }

   readonly EndpointConfiguration _configuration;
   readonly IDependencyInjectionContainer _container;
   readonly IRootResolver _rootResolver;

   public Endpoint(IDependencyInjectionContainer container,
                   ITessagingRouter tessagingRouter,
                   IEndpointRegistry endpointRegistry,
                   EndpointConfiguration configuration)
   {
      Argument.NotNull(container).NotNull(configuration);
      _container = container;
      _rootResolver = container.Resolver;
      _tessagingRouter = tessagingRouter;
      _configuration = configuration;
      _endpointRegistry = endpointRegistry;
   }

   public EndpointId Id => _configuration.Id;
   public IRootResolver ServiceLocator => _rootResolver;

   public EndPointAddress? Address => _serverComponents?.Inbox.Address;
   public EndPointAddress? TypermediaAddress => _typermediaTransportServer?.Address is { } uri ? new EndPointAddress(uri) : null;
   readonly ITessagingRouter _tessagingRouter;
   readonly IEndpointRegistry _endpointRegistry;

   ServerComponents? _serverComponents;
#pragma warning disable CA2213 // Disposed by the DI container
   ITypermediaTransportServer? _typermediaTransportServer;
#pragma warning restore CA2213

   public bool IsRunning => _isListening && _isSending;
   bool _isListening = false;
   bool _isSending = false;

   public async Task StartListeningComponentsAsync()
   {
      State.Assert(!_isListening);
      this.Log().Info($"Endpoint '{_configuration.Name}' ({Id}) starting listening components");
      _isListening = true;

      RunSanityChecks();

      _serverComponents = new ServerComponents(_rootResolver.Resolve<TommandScheduler>(), _rootResolver.Resolve<IInbox>(), _rootResolver.Resolve<IOutbox>());
      _typermediaTransportServer = _rootResolver.Resolve<ITypermediaTransportServer>();

      await Task.WhenAll(_serverComponents.Inbox.StartAsync(), _serverComponents.TommandScheduler.StartAsync(), _typermediaTransportServer.StartAsync()).caf();

      this.Log().Info($"Endpoint '{_configuration.Name}' ({Id}) listening at {Address} (tessaging) and {TypermediaAddress} (typermedia)");
   }

   public async Task StartSendingComponentsAsync()
   {
      State.Assert(!_isSending);
      this.Log().Info($"Endpoint '{_configuration.Name}' ({Id}) starting sending components");
      _isSending = true;
      if(_serverComponents != null)
      {
         //Tessaging connects to all endpoints including ourselves. Scheduled tommands need to dispatch over the remote protocol to get the delivery guarantees...
         var serverAddresses = _endpointRegistry.ServerEndpointAddresses.ToHashSet();
         serverAddresses.Add(_serverComponents.Inbox.Address);
         await Task.WhenAll(serverAddresses.Select(address => _tessagingRouter.ConnectAsync(address))).caf();
         await Task.WhenAll(_serverComponents.Outbox.StartAsync()).caf();
      }
   }

   static void RunSanityChecks() => AssertAllTypesNeedingMappingsAreMapped();

   //todo: figure out how to do this sanely
   static void AssertAllTypesNeedingMappingsAreMapped() {}

   public async Task StopSendingComponentsAsync()
   {
      if(_isSending)
      {
         this.Log().Info($"Endpoint '{_configuration.Name}' ({Id}) stopping sending components");
         _isSending = false;
         if(_serverComponents != null)
         {
            _serverComponents.TommandScheduler.Stop();
            await _serverComponents.Outbox.StopAsync().caf();
         }
      }
   }

   public async Task StopListeningComponentsAsync()
   {
      if(_isListening)
      {
         this.Log().Info($"Endpoint '{_configuration.Name}' ({Id}) stopping listening components");
         _isListening = false;
         if(_serverComponents != null)
            await _serverComponents.Inbox.StopAsync().caf();

         if(_typermediaTransportServer != null)
            await _typermediaTransportServer.StopAsync().caf();

         _tessagingRouter.Stop();
      }
   }

   public async ValueTask DisposeAsync()
   {
      this.Log().Debug($"Endpoint '{_configuration.Name}' ({Id}) disposing");
      await StopSendingComponentsAsync().caf();
      await StopListeningComponentsAsync().caf();
      if(_serverComponents != null)
      {
         var exceptionReporter = _rootResolver.Resolve<IBackgroundExceptionReporter>();
         await _container.DisposeAsync().caf();
         _serverComponents.Dispose();
         exceptionReporter.ThrowIfAnyExceptions();
      }
   }
}
