using System;
using System.Linq;
using System.Threading.Tasks;
using Compze.DependencyInjection;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Buses.Implementation;
using static Compze.Contracts.Assert;

namespace Compze.Tessaging.Buses;

class Endpoint : IEndpoint
{
   class ServerComponents(CommandScheduler commandScheduler, IInbox inbox, IOutbox outbox) : IDisposable
   {
      readonly CommandScheduler _commandScheduler = commandScheduler;
      public readonly IInbox Inbox = inbox;
      readonly IOutbox _outbox = outbox;

      public async Task InitAsync() => await Task.WhenAll(Inbox.StartAsync(), _commandScheduler.StartAsync(), _outbox.StartAsync()).caf();
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
                   IGlobalBusStateTracker globalStateTracker,
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
   readonly IGlobalBusStateTracker _globalStateTracker;
   readonly ITransport _transport;
   readonly IEndpointRegistry _endpointRegistry;

   ServerComponents? _serverComponents;

   public async Task InitAsync()
   {
      State.Is(!IsRunning);

      RunSanityChecks();

      //todo: find cleaner way of handling what an endpoint supports
      if(!_configuration.IsPureClientEndpoint)
      {
         _serverComponents = new ServerComponents(ServiceLocator.Resolve<CommandScheduler>(), ServiceLocator.Resolve<IInbox>(), ServiceLocator.Resolve<IOutbox>());

         await _serverComponents.InitAsync().caf();
      }


      IsRunning = true;
   }

   public async Task ConnectAsync()
   {
      var serverEndpoints = _endpointRegistry.ServerEndpoints.ToHashSet();
      if (_serverComponents != null)
      {
         serverEndpoints.Add(_serverComponents.Inbox.Address); //Yes, we do connect to ourselves. Scheduled commands need to dispatch over the remote protocol to get the delivery guarantees...
      }
      await Task.WhenAll(serverEndpoints.Select(address => _transport.ConnectAsync(address))).caf();
   }

   static void RunSanityChecks() => AssertAllTypesNeedingMappingsAreMapped();

   //todo: figure out how to do this sanely
   static void AssertAllTypesNeedingMappingsAreMapped()
   {
   }

   public async Task StopAsync()
   {
      State.Is(IsRunning);
      IsRunning = false;
      _transport.Stop();
      if(_serverComponents != null )
         await _serverComponents.StopAsync().caf();
   }

   public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride) => _globalStateTracker.AwaitNoMessagesInFlight(timeoutOverride);

   public async ValueTask DisposeAsync()
   {
      if(IsRunning) await StopAsync().caf();
      await ServiceLocator.DisposeAsync().caf();
      _serverComponents?.Dispose();
   }
}