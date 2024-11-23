using System;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.Messaging.Buses.Implementation;

partial class Inbox : IInbox, IDisposable
{
   Runner? _runner;
   readonly RealEndpointConfiguration _configuration;

   readonly string _address;
   readonly ITypeMapper _typeMapper;
   readonly IRemotableMessageSerializer _serializer;
   readonly IMessageStorage _storage;
   readonly HandlerExecutionEngine _handlerExecutionEngine;
   readonly AspNetHost _aspNetHost;

   public Inbox(IServiceLocator serviceLocator, IGlobalBusStateTracker globalStateTracker, IMessageHandlerRegistry handlerRegistry, RealEndpointConfiguration configuration, IMessageStorage messageStorage, ITypeMapper typeMapper, ITaskRunner taskRunner, IRemotableMessageSerializer serializer)
   {
      _configuration = configuration;
      _typeMapper = typeMapper;
      _serializer = serializer;
      _address = configuration.Address;
      _storage = messageStorage;
      _handlerExecutionEngine = new HandlerExecutionEngine(globalStateTracker, handlerRegistry, serviceLocator, _storage, taskRunner);
      _aspNetHost = new AspNetHost(_handlerExecutionEngine, _storage, _address, _configuration, _typeMapper, _serializer);
   }

   public EndPointAddress Address => _runner?.Address ?? new EndPointAddress(_address);

   public async Task StartAsync()
   {
      Assert.State.Assert(_runner is null);
      var storageStartTask = _storage.StartAsync();

      _runner = new Runner(_handlerExecutionEngine, _storage, _address, _configuration, _typeMapper, _serializer);
      await Task.WhenAll(_runner.StartAsync(), storageStartTask, _aspNetHost.StartAsync()).NoMarshalling();
   }

   //todo: Should be async
   public void Stop()
   {
      Assert.State.Assert(_runner is not null);
      _runner.Dispose();
      //_aspNetHost.StopAsync().SyncResult();
      _runner = null;
   }


   public void Dispose()
   {
      if(_runner is not null)
      {
         Stop();
      }
   }


}