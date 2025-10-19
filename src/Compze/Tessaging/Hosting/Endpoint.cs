using System;
using System.Linq;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Threading.TasksCE;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Tessaging.Hosting;

class Endpoint : IEndpoint
{
   class ServerComponents(CommandScheduler commandScheduler, IInbox inbox, IOutbox outbox) : IDisposable
   {
      readonly CommandScheduler _commandScheduler = commandScheduler;
      public readonly IInbox Inbox = inbox;
      readonly IOutbox _outbox = outbox;

      public async Task StartListeningComponentsAsync() => await Task.WhenAll(Inbox.StartAsync(), _commandScheduler.StartAsync()).caf();
      public async Task StartSendingComponentsAsync() => await Task.WhenAll(_outbox.StartAsync()).caf();

      public async Task StopSendingComponentsAsync()
      {
         _commandScheduler.Stop();
         await _outbox.StopAsync().caf();
      }

      public async Task StopListeningComponentsAsync()
      {
         await Inbox.StopAsync().caf();
      }

      public void Dispose() => _commandScheduler.Dispose();
   }

   readonly EndpointConfiguration _configuration;

   public Endpoint(IServiceLocator serviceLocator,
                   IMessagesInFlightTracker globalStateTracker,
                   ITransport transport,
                   IEndpointRegistry endpointRegistry,
                   EndpointConfiguration configuration)
   {
      Argument.NotNull(serviceLocator).NotNull(configuration);
      ServiceLocator = serviceLocator;
      _globalStateTracker = globalStateTracker;
      _transport = transport;
      _configuration = configuration;
      _endpointRegistry = endpointRegistry;
   }

   public EndpointId Id => _configuration.Id;
   public IServiceLocator ServiceLocator { get; }

   public EndPointAddress? Address => _serverComponents?.Inbox.Address;
   readonly IMessagesInFlightTracker _globalStateTracker;
   readonly ITransport _transport;
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

      _transport.Start();

      //todo: find cleaner way of handling what an endpoint supports
      if(!_configuration.IsPureClientEndpoint)
      {
         _serverComponents = new ServerComponents(ServiceLocator.Resolve<CommandScheduler>(), ServiceLocator.Resolve<IInbox>(), ServiceLocator.Resolve<IOutbox>());

         await _serverComponents.StartListeningComponentsAsync().caf();
      }
   }

   public async Task StartSendingComponentsAsync()
   {
      State.Is(!_isSending);
      _isSending = true;
      var serverEndpoints = _endpointRegistry.ServerEndpoints.ToHashSet();
      await Task.WhenAll(serverEndpoints.Select(address => _transport.ConnectAsync(address))).caf();
      if(_serverComponents != null)
      {
         await _serverComponents.StartSendingComponentsAsync().caf();
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
            await _serverComponents.StopSendingComponentsAsync().caf();
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
            await _serverComponents.StopListeningComponentsAsync().caf();
         }

         _transport.Stop();
      }
   }

   public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride) => _globalStateTracker.AwaitNoMessagesInFlight(timeoutOverride);

   public async ValueTask DisposeAsync()
   {
      State.Is(!_isListening).Is(!_isSending);
      if(_serverComponents != null)
      {
         // Check for any exceptions collected on background threads before disposing
         if(!_configuration.IsPureClientEndpoint)
         {
            var exceptionReporter = ServiceLocator.Resolve<IBackgroundExceptionReporter>();
            exceptionReporter.ThrowIfAnyExceptions();
         }

         await ServiceLocator.DisposeAsync().caf();
         _serverComponents.Dispose();
      }
   }
}
