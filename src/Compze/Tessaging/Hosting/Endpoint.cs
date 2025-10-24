using System;
using System.Linq;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Abstractions.Transport;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Tessaging.Hosting.Implementation.Abstractions.MessageHandling;
using Compze.Tessaging.Hosting.Implementation.Abstractions.Transport;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Threading.TasksCE;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Tessaging.Hosting;

class Endpoint : IEndpoint
{
   class ServerComponents(CommandScheduler commandScheduler, IInbox inbox, IOutbox outbox) : IDisposable
   {
      public readonly CommandScheduler CommandScheduler = commandScheduler;
      public readonly IInbox Inbox = inbox;
      public readonly IOutbox Outbox = outbox;

      public void Dispose() => CommandScheduler.Dispose();
   }

   readonly EndpointConfiguration _configuration;

   public Endpoint(IServiceLocator serviceLocator,
                   IMessagesInFlightTracker globalStateTracker,
                   ITransportClient transportClient,
                   IEndpointRegistry endpointRegistry,
                   EndpointConfiguration configuration)
   {
      Argument.NotNull(serviceLocator).NotNull(configuration);
      ServiceLocator = serviceLocator;
      _globalStateTracker = globalStateTracker;
      _transportClient = transportClient;
      _configuration = configuration;
      _endpointRegistry = endpointRegistry;
   }

   public EndpointId Id => _configuration.Id;
   public IServiceLocator ServiceLocator { get; }

   public EndPointAddress? Address => _serverComponents?.Inbox.Address;
   readonly IMessagesInFlightTracker _globalStateTracker;
   readonly ITransportClient _transportClient;
   readonly IEndpointRegistry _endpointRegistry;

   ServerComponents? _serverComponents;

   public bool IsRunning => _isListening && _isSending;
   bool _isListening = false;
   bool _isSending = false;

   public async Task StartListeningComponentsAsync()
   {
      State.Is(!_isListening);
      _isListening = true;

      RunSanityChecks();

      _transportClient.Start();

      //todo: find cleaner way of handling what an endpoint supports
      if(!_configuration.IsPureClientEndpoint)
      {
         _serverComponents = new ServerComponents(ServiceLocator.Resolve<CommandScheduler>(), ServiceLocator.Resolve<IInbox>(), ServiceLocator.Resolve<IOutbox>());

         await Task.WhenAll(_serverComponents.Inbox.StartAsync(), _serverComponents.CommandScheduler.StartAsync()).caf();
      }
   }

   public async Task StartSendingComponentsAsync()
   {
      State.Is(!_isSending);
      _isSending = true;
      var serverEndpoints = _endpointRegistry.ServerEndpoints.ToHashSet();
      await Task.WhenAll(serverEndpoints.Select(address => _transportClient.ConnectAsync(address))).caf();
      if(_serverComponents != null)
      {
         await Task.WhenAll(_serverComponents.Outbox.StartAsync()).caf();
         serverEndpoints.Add(_serverComponents.Inbox.Address); //Yes, we do connect to ourselves. Scheduled commands need to dispatch over the remote protocol to get the delivery guarantees...
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
            _serverComponents.CommandScheduler.Stop();
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

         _transportClient.Stop();
      }
   }

   public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride) => _globalStateTracker.AwaitNoMessagesInFlight(timeoutOverride);

   public async ValueTask DisposeAsync()
   {
      await StopSendingComponentsAsync().caf();
      await StopListeningComponentsAsync().caf();
      if(_serverComponents != null)
      {
         var exceptionReporter = ServiceLocator.Resolve<IBackgroundExceptionReporter>();
         await ServiceLocator.DisposeAsync().caf();
         _serverComponents.Dispose();
         // Check for any exceptions collected on background threads before disposing
         if(!_configuration.IsPureClientEndpoint)
         {
            exceptionReporter.ThrowIfAnyExceptions();
         }
      }
   }
}
