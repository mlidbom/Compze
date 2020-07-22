﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Messaging.NetMQCE;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using Composable.System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Outbox
    {
        internal class InboxConnection : IInboxConnection
        {
            readonly IThreadShared<InboxConnectionState> _state;
            public MessageTypes.Internal.EndpointInformation EndpointInformation { get; private set; }
            readonly ITypeMapper _typeMapper;
            readonly IRemotableMessageSerializer _serializer;

            public async Task SendAsync(MessageTypes.Remotable.ExactlyOnce.IEvent @event)
            {
                var taskCompletionSource = new AsyncTaskCompletionSource();
                var outGoingMessage = TransportMessage.OutGoing.Create(@event, _typeMapper, _serializer);

                _state.WithExclusiveAccess(state =>
                {
                    state.ExpectedCompletionTasks.Add(outGoingMessage.MessageId, taskCompletionSource);
                    SendMessage(state, outGoingMessage);
                });

                await taskCompletionSource.Task.NoMarshalling();
            }

            public async Task SendAsync(MessageTypes.Remotable.ExactlyOnce.ICommand command)
            {
                var taskCompletionSource = new AsyncTaskCompletionSource();
                var outGoingMessage = TransportMessage.OutGoing.Create(command, _typeMapper, _serializer);

                _state.WithExclusiveAccess(state =>
                {
                    state.ExpectedCompletionTasks.Add(outGoingMessage.MessageId, taskCompletionSource);
                    SendMessage(state, outGoingMessage);
                });

                await taskCompletionSource.Task.NoMarshalling();
            }

            public async Task<TCommandResult> PostAsync<TCommandResult>(MessageTypes.Remotable.AtMostOnce.ICommand<TCommandResult> command)
            {
                var taskCompletionSource = new AsyncTaskCompletionSource<Func<object>>();
                var outGoingMessage = TransportMessage.OutGoing.Create(command, _typeMapper, _serializer);

                _state.WithExclusiveAccess(state =>
                {
                    state.ExpectedResponseTasks.Add(outGoingMessage.MessageId, taskCompletionSource);
                    SendMessage(state, outGoingMessage);
                });

                return (TCommandResult)(await taskCompletionSource.Task.NoMarshalling()).Invoke();
            }

            public async Task PostAsync(MessageTypes.Remotable.AtMostOnce.ICommand command)
            {
                var taskCompletionSource = new AsyncTaskCompletionSource();
                var outGoingMessage = TransportMessage.OutGoing.Create(command, _typeMapper, _serializer);

                _state.WithExclusiveAccess(state =>
                {
                    state.ExpectedCompletionTasks.Add(outGoingMessage.MessageId, taskCompletionSource);
                    SendMessage(state, outGoingMessage);
                });

                await taskCompletionSource.Task.NoMarshalling();
            }

            public async Task<TQueryResult> GetAsync<TQueryResult>(MessageTypes.Remotable.NonTransactional.IQuery<TQueryResult> query)
            {
                var taskCompletionSource = new AsyncTaskCompletionSource<Func<object>>();
                var outGoingMessage = TransportMessage.OutGoing.Create(query, _typeMapper, _serializer);

                _state.WithExclusiveAccess(state =>
                {
                    state.ExpectedResponseTasks.Add(outGoingMessage.MessageId, taskCompletionSource);
                    SendMessage(state, outGoingMessage);
                });

                return (TQueryResult)(await taskCompletionSource.Task.NoMarshalling()).Invoke();
            }

            static void SendMessage(InboxConnectionState @this, TransportMessage.OutGoing outGoingMessage)
            {
                @this.GlobalBusStateTracker.SendingMessageOnTransport(outGoingMessage);
                @this.SendQueue.Enqueue(outGoingMessage);
            }

            internal async Task Init() { EndpointInformation = await GetAsync(new MessageTypes.Internal.EndpointInformationQuery()).NoMarshalling(); }

#pragma warning disable 8618 //Refactor: This really should not be suppressed. We do have a bad design that might cause null reference exceptions here if Init has not been called.
            internal InboxConnection(IGlobalBusStateTracker globalBusStateTracker,
#pragma warning restore 8618
                                     EndPointAddress serverEndpoint,
                                     NetMQPoller poller,
                                     ITypeMapper typeMapper,
                                     IRemotableMessageSerializer serializer)
            {
                _serializer = serializer;
                _typeMapper = typeMapper;

#pragma warning disable CA2000 // Dispose objects before losing scope
                _state = new OptimizedThreadShared<InboxConnectionState>(new InboxConnectionState(new DealerSocket(), globalBusStateTracker));
#pragma warning restore CA2000 // Dispose objects before losing scope

                _state.WithExclusiveAccess(state =>
                {
                    poller.Add(state.SendQueue);

                    state.SendQueue.ReceiveReady += SendQueuedMessages;

                    //Should we screw up with the pipelining we prefer performance problems (memory usage) to lost messages or blocking
                    state.Socket.Options.SendHighWatermark = int.MaxValue;
                    state.Socket.Options.ReceiveHighWatermark = int.MaxValue;

                    //We guarantee delivery upon restart in other ways. When we shut down, just do it.
                    state.Socket.Options.Linger = 0.Milliseconds();

                    state.Socket.ReceiveReady += ReceiveResponse;

                    state.Socket.Connect(serverEndpoint);
                    poller.Add(state.Socket);
                });
            }

            void SendQueuedMessages(object sender, NetMQQueueEventArgs<TransportMessage.OutGoing> netMQQueueEventArgs) => _state.WithExclusiveAccess(state =>
            {
                while(netMQQueueEventArgs.Queue.TryDequeue(out var message, TimeSpan.Zero)) state.Socket.Send(message);
            });

            public void Dispose() => _state.WithExclusiveAccess(state => state.Dispose());

            class InboxConnectionState : IDisposable
            {
                internal readonly IGlobalBusStateTracker GlobalBusStateTracker;
                internal readonly Dictionary<Guid, AsyncTaskCompletionSource<Func<object>>> ExpectedResponseTasks = new Dictionary<Guid, AsyncTaskCompletionSource<Func<object>>>();
                internal readonly Dictionary<Guid, AsyncTaskCompletionSource> ExpectedCompletionTasks = new Dictionary<Guid, AsyncTaskCompletionSource>();
                internal readonly DealerSocket Socket;
                internal readonly NetMQQueue<TransportMessage.OutGoing> SendQueue = new NetMQQueue<TransportMessage.OutGoing>();

                public InboxConnectionState(DealerSocket socket, IGlobalBusStateTracker globalBusStateTracker)
                {
                    Socket = socket;
                    GlobalBusStateTracker = globalBusStateTracker;
                }

                public void Dispose()
                {
                    Socket.Dispose();
                    SendQueue.Dispose();
                }
            }

            //Runs on poller thread so NO BLOCKING HERE!
            void ReceiveResponse(object sender, NetMQSocketEventArgs e)
            {
                var responseBatch = TransportMessage.Response.Incoming.ReceiveBatch(e.Socket, _typeMapper, _serializer, batchMaximum: 100);

                _state.WithExclusiveAccess(state =>
                {
                    foreach(var response in responseBatch)
                    {
                        switch(response.ResponseType)
                        {
                            case TransportMessage.Response.ResponseType.Received:
                            case TransportMessage.Response.ResponseType.Success:
                                var successResponse = state.ExpectedCompletionTasks.GetAndRemove(response.RespondingToMessageId);
                                successResponse.SetResult();
                                break;
                            case TransportMessage.Response.ResponseType.SuccessWithData:
                                var successResponseWithData = state.ExpectedResponseTasks.GetAndRemove(response.RespondingToMessageId);
                                successResponseWithData.SetResult(response.DeserializeResult);
                                break;
                            case TransportMessage.Response.ResponseType.Failure:
                                var failureResponse = state.ExpectedCompletionTasks.GetAndRemove(response.RespondingToMessageId);
                                failureResponse.SetException(new MessageDispatchingFailedException(response.Body ?? "Got no exception text from remote end."));
                                break;
                            case TransportMessage.Response.ResponseType.FailureExpectedReturnValue:
                                var failureResponseExpectingData = state.ExpectedResponseTasks.GetAndRemove(response.RespondingToMessageId);
                                failureResponseExpectingData.SetException(new MessageDispatchingFailedException(response.Body ?? "Got no exception text from remote end."));
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                });
            }
        }
    }
}