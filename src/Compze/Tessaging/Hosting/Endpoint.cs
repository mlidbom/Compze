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
   class ServerComponents(CommandScheduler commandScheduler, IInbox inbox) : IDisposable
   {
      readonly CommandScheduler _commandScheduler = commandScheduler;
      public readonly IInbox Inbox = inbox;

      public async Task InitAsync() => await Task.WhenAll(Inbox.StartAsync(), _commandScheduler.StartAsync()).caf();

      public async Task StopAsync()
      {
         _commandScheduler.Stop();
         await Inbox.StopAsync().caf();
      }

      public void Dispose() => _commandScheduler.Dispose();
   }

   readonly EndpointConfiguration _configuration;
   public bool IsRunning { get; private set; }

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

   public async Task StartListeningComponentsAsync()
   {
      State.Is(!IsRunning);

      RunSanityChecks();

      _transport.Start();

      //todo: find cleaner way of handling what an endpoint supports
      if(!_configuration.IsPureClientEndpoint)
      {
         _serverComponents = new ServerComponents(ServiceLocator.Resolve<CommandScheduler>(), ServiceLocator.Resolve<IInbox>());

         await _serverComponents.InitAsync().caf();
      }

      IsRunning = true;
   }

   public async Task StartSendingComponentsAsync()
   {
      var serverEndpoints = _endpointRegistry.ServerEndpoints.ToHashSet();
      await Task.WhenAll(serverEndpoints.Select(address => _transport.ConnectAsync(address))).caf();
      if(_serverComponents != null)
      {
         await ServiceLocator.Resolve<IOutbox>().StartAsync().caf();
         serverEndpoints.Add(_serverComponents.Inbox.Address); //Yes, we do connect to ourselves. Scheduled commands need to dispatch over the remote protocol to get the delivery guarantees...
      }
   }

   static void RunSanityChecks() => AssertAllTypesNeedingMappingsAreMapped();

   //todo: figure out how to do this sanely
   static void AssertAllTypesNeedingMappingsAreMapped() {}

   public async Task StopAsync()
   {
      State.Is(IsRunning);
      IsRunning = false;
      _transport.Stop();
      if(_serverComponents != null)
         await _serverComponents.StopAsync().caf();
   }

   public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride) => _globalStateTracker.AwaitNoMessagesInFlight(timeoutOverride);

   public async ValueTask DisposeAsync()
   {
      if(IsRunning) await StopAsync().caf();

      // Check for any exceptions collected on background threads before disposing
      if(!_configuration.IsPureClientEndpoint)
      {
         var exceptionReporter = ServiceLocator.Resolve<IBackgroundExceptionReporter>();
         exceptionReporter.ThrowIfAnyExceptions();
      }

      await ServiceLocator.DisposeAsync().caf();
      _serverComponents?.Dispose();
   }
}
