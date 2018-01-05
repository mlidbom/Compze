﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.System;
using Composable.System.Data.SqlClient;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using NetMQ;
using NetMQ.Sockets;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Inbox : IInbox, IDisposable
    {
        readonly IGlobalBusStateTracker _globalStateTracker;

        readonly IResourceGuard _resourceGuard = ResourceGuard.WithTimeout(1.Seconds());

        bool _running;
        string _address;

        readonly NetMQQueue<TransportMessage.Response.Outgoing> _responseQueue = new NetMQQueue<TransportMessage.Response.Outgoing>();

        RouterSocket _responseSocket;

        public IReadOnlyList<Exception> ThrownExceptions => _globalStateTracker.GetExceptionsFor(this);
        NetMQPoller _poller;
        readonly MessageStorage _storage;
        readonly HandlerExecutionEngine _handlerExecutionEngine;

        public Inbox(IServiceLocator serviceLocator, IGlobalBusStateTracker globalStateTracker, IMessageHandlerRegistry handlerRegistry, EndpointConfiguration configuration, ISqlConnection connectionFactory)
        {
            _address = configuration.Address;
            _globalStateTracker = globalStateTracker;
            _storage = new MessageStorage(connectionFactory);
            _handlerExecutionEngine = new HandlerExecutionEngine(this, globalStateTracker, handlerRegistry, serviceLocator, _storage);
        }

        public EndPointAddress Address => new EndPointAddress(_address);

        public void Start() => _resourceGuard.Update(action: () =>
        {
            Contract.Invariant.Assert(!_running);
            _running = true;

            _responseSocket = new RouterSocket();
            //Should we screw up with the pipelining we prefer performance problems (memory usage) to lost messages or blocking
            _responseSocket.Options.SendHighWatermark = int.MaxValue;
            _responseSocket.Options.ReceiveHighWatermark = int.MaxValue;

            //We guarantee delivery upon restart in other ways. When we shut down, just do it.
            _responseSocket.Options.Linger = 0.Milliseconds();

            _address = _responseSocket.BindAndReturnActualAddress(_address);
            _responseSocket.ReceiveReady += HandleIncomingMessage;

            _responseQueue.ReceiveReady += SendResponseMessage;

            _poller = new NetMQPoller() {_responseSocket, _responseQueue};
            _poller.RunAsync();

            _handlerExecutionEngine.Start();
            _storage.Start();
        });

        void SendResponseMessage(object sender, NetMQQueueEventArgs<TransportMessage.Response.Outgoing> e)
        {
            while(e.Queue.TryDequeue(out var response, TimeSpan.Zero))
            {
                _responseSocket.Send(response);
            }
        }

        void HandleIncomingMessage(object sender, NetMQSocketEventArgs e)
        {
            Contract.Argument.Assert(e.IsReadyToReceive);
            var transportMessage = TransportMessage.InComing.Receive(_responseSocket);

            var dispatchTask = DispatchAsync(transportMessage);

            dispatchTask.ContinueWith(dispatchResult =>
            {
                var message = transportMessage.DeserializeMessageAndCacheForNextCall();
                if(message.RequiresResponse())
                {
                    if(dispatchResult.IsFaulted)
                    {
                        _responseQueue.Enqueue(transportMessage.CreateFailureResponse(dispatchResult.Exception));
                    } else if(dispatchResult.IsCompleted)
                    {
                        _responseQueue.Enqueue(transportMessage.CreateSuccessResponse(dispatchResult.Result));
                    }
                }
            });
        }

        public void Stop()
        {
            Contract.Invariant.Assert(_running);
            _running = false;
            _poller.Dispose();
            _responseSocket.Close();
            _responseSocket.Dispose();
            _handlerExecutionEngine.Stop();
        }

        //Todo sane exception handling for this background task.
        async Task<object> DispatchAsync(TransportMessage.InComing message) => await Task.Run(async () =>
        {
            var innerMessage = message.DeserializeMessageAndCacheForNextCall();
            if(innerMessage is ITransactionalExactlyOnceDeliveryMessage)
            {
                _storage.SaveMessage(message);
                _responseQueue.Enqueue(message.CreatePersistedResponse());
            }

            return await _handlerExecutionEngine.Enqueue(message);
        });

        public void Dispose()
        {
            if(_running)
                Stop();
        }
    }
}
