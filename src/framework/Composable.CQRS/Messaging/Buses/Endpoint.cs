using System;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.Messaging.Buses.Implementation;
using Composable.SystemCE.CollectionsCE.GenericCE;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.Messaging.Buses;

class Endpoint : IEndpoint
{
   class ServerComponents : IDisposable
   {
      readonly CommandScheduler _commandScheduler;
      public readonly IInbox Inbox;
      readonly IOutbox _outbox;

      public ServerComponents(CommandScheduler commandScheduler, IInbox inbox, IOutbox outbox)
      {
         _commandScheduler = commandScheduler;
         Inbox = inbox;
         _outbox = outbox;
      }

      public async Task InitAsync() => await Task.WhenAll(Inbox.StartAsync(), _commandScheduler.StartAsync(), _outbox.StartAsync()).CaF();
      public async Task StopAsync()
      {
         _commandScheduler.Stop();
         await Inbox.StopAsync().CaF();
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
      Contract.ArgumentNotNull(serviceLocator, nameof(serviceLocator), configuration, nameof(configuration));
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
      Assert.State.Assert(!IsRunning);

      RunSanityChecks();

      //todo: find cleaner way of handling what an endpoint supports
      if(!_configuration.IsPureClientEndpoint)
      {
         _serverComponents = new ServerComponents(ServiceLocator.Resolve<CommandScheduler>(), ServiceLocator.Resolve<IInbox>(), ServiceLocator.Resolve<IOutbox>());

         await _serverComponents.InitAsync().CaF();
      }


      IsRunning = true;
   }

   public async Task ConnectAsync()
   {
      var serverEndpoints = _endpointRegistry.ServerEndpoints.ToSet();
      if (_serverComponents != null)
      {
         serverEndpoints.Add(_serverComponents.Inbox.Address); //Yes, we do connect to ourselves. Scheduled commands need to dispatch over the remote protocol to get the delivery guarantees...
      }
      await Task.WhenAll(serverEndpoints.Select(address => _transport.ConnectAsync(address))).CaF();
   }

   static void RunSanityChecks() => AssertAllTypesNeedingMappingsAreMapped();

   //todo: figure out how to do this sanely
   static void AssertAllTypesNeedingMappingsAreMapped()
   {
   }

   public async Task StopAsync()
   {
      Assert.State.Assert(IsRunning);
      IsRunning = false;
      _transport.Stop();
      if(_serverComponents != null )
         await _serverComponents.StopAsync().CaF();
   }

   public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride) => _globalStateTracker.AwaitNoMessagesInFlight(timeoutOverride);

   public async ValueTask DisposeAsync()
   {
      if(IsRunning) await StopAsync().CaF();
      await ServiceLocator.DisposeAsync().CaF();
      _serverComponents?.Dispose();
   }
}