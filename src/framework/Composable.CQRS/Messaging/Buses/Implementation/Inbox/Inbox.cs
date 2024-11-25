using System;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.Messaging.Buses.Implementation;

partial class Inbox : IInbox, IAsyncDisposable
{
   Runner? _runner;
   readonly RealEndpointConfiguration _configuration;

   readonly string _address;
   readonly ITypeMapper _typeMapper;
   readonly IRemotableMessageSerializer _serializer;
   readonly IMessageStorage _storage;
   readonly HandlerExecutionEngine _handlerExecutionEngine;
   readonly AspNetHost _aspNetHost;

   public Inbox(IServiceLocator serviceLocator, HandlerExecutionEngine handlerExecutionEngine, IGlobalBusStateTracker globalStateTracker, IMessageHandlerRegistry handlerRegistry, RealEndpointConfiguration configuration, IMessageStorage messageStorage, ITypeMapper typeMapper, ITaskRunner taskRunner, IRemotableMessageSerializer serializer, IDependencyInjectionContainer container)
   {
      _configuration = configuration;
      _typeMapper = typeMapper;
      _serializer = serializer;
      _address = configuration.Address;
      _storage = messageStorage;
      _handlerExecutionEngine = handlerExecutionEngine;
      _aspNetHost = new AspNetHost(_handlerExecutionEngine, _storage, _address, _configuration, _typeMapper, _serializer, serviceLocator, container);
   }

   public EndPointAddress Address => new EndPointAddress(netMqAddress: _runner!.Address, aspNetAddress: _aspNetHost.Address); //?? new EndPointAddress(_address);

   public async Task StartAsync()
   {
      Assert.State.Assert(_runner is null);
      var storageStartTask = _storage.StartAsync();

      _runner = new Runner(_handlerExecutionEngine, _storage, _address, _configuration, _typeMapper, _serializer);
      await Task.WhenAll(_runner.StartAsync(), storageStartTask, _aspNetHost.StartAsync()).NoMarshalling();
   }

   public async Task StopAsync()
   {
      if(_runner is null) return;
      _runner.Dispose();
      await _aspNetHost.StopAsync().NoMarshalling();
      _runner = null;
   }


   public async ValueTask DisposeAsync()
   {
      await StopAsync().NoMarshalling();
      await _aspNetHost.DisposeAsync().NoMarshalling();
   }
}