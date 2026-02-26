using System;
using System.Linq;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.Implementation.Transport.Client.Routing.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Compze.Contracts;
using static Compze.Contracts.Contract;

namespace Compze.Tessaging.Hosting;

public class Endpoint : IEndpoint
{
   public class ServerComponents(TommandScheduler tommandScheduler, IInbox inbox, IOutbox outbox) : IDisposable
   {
      internal readonly TommandScheduler TommandScheduler = tommandScheduler;
      internal readonly IInbox Inbox = inbox;
      internal readonly IOutbox Outbox = outbox;

      public void Dispose() => TommandScheduler.Dispose();
   }

   readonly EndpointConfiguration _configuration;

   public Endpoint(IServiceLocator serviceLocator,
                   ITessagesInFlightTracker globalStateTracker,
                   ITessagingRouter tessagingRouter,
                   IEndpointRegistry endpointRegistry,
                   EndpointConfiguration configuration)
   {
      Argument.NotNull(serviceLocator).NotNull(configuration);
      ServiceLocator = serviceLocator;
      _globalStateTracker = globalStateTracker;
      _tessagingRouter = tessagingRouter;
      _configuration = configuration;
      _endpointRegistry = endpointRegistry;
   }

   public EndpointId Id => _configuration.Id;
   public IServiceLocator ServiceLocator { get; }

   public EndPointAddress? Address => _serverComponents?.Inbox.Address;
   readonly ITessagesInFlightTracker _globalStateTracker;
   readonly ITessagingRouter _tessagingRouter;
   readonly IEndpointRegistry _endpointRegistry;

   ServerComponents? _serverComponents;

   public bool IsRunning => _isListening && _isSending;
   bool _isListening = false;
   bool _isSending = false;

   public async Task StartListeningComponentsAsync()
   {
      State.Assert(!_isListening);
      _isListening = true;

      RunSanityChecks();

      _serverComponents = new ServerComponents(ServiceLocator.Resolve<TommandScheduler>(), ServiceLocator.Resolve<IInbox>(), ServiceLocator.Resolve<IOutbox>());

      await Task.WhenAll(_serverComponents.Inbox.StartAsync(), _serverComponents.TommandScheduler.StartAsync()).caf();
   }

   public async Task StartSendingComponentsAsync()
   {
      State.Assert(!_isSending);
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
         _isListening = false;
         if(_serverComponents != null)
         {
            await _serverComponents.Inbox.StopAsync().caf();
         }

         _tessagingRouter.Stop();
      }
   }

   public void AwaitNoTessagesInFlight(TimeSpan? timeoutOverride) => _globalStateTracker.AwaitNoTessagesInFlight(timeoutOverride);

   public async ValueTask DisposeAsync()
   {
      await StopSendingComponentsAsync().caf();
      await StopListeningComponentsAsync().caf();
      if(_serverComponents != null)
      {
         var exceptionReporter = ServiceLocator.Resolve<IBackgroundExceptionReporter>();
         await ServiceLocator.DisposeAsync().caf();
         _serverComponents.Dispose();
         exceptionReporter.ThrowIfAnyExceptions();
      }
   }
}
