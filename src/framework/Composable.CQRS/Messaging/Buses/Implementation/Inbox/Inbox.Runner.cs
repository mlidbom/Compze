using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;
using NetMQ;
using NetMQ.Sockets;

namespace Composable.Messaging.Buses.Implementation;

partial class Inbox
{
   class Runner : IDisposable
   {
      readonly NetMQQueue<NetMQMessage> _responseQueue;
      readonly RouterSocket _serverSocket;
      readonly NetMQPoller _poller;
      readonly Thread _messageReceiverThread;
      readonly CancellationTokenSource _cancellationTokenSource;
      readonly BlockingCollection<IReadOnlyList<TransportMessage.InComing>> _receivedMessageBatches = new();
      readonly HandlerExecutionEngine _handlerExecutionEngine;
      readonly IMessageStorage _storage;
      readonly RealEndpointConfiguration _configuration;
      readonly ITypeMapper _typeMapper;
      readonly IRemotableMessageSerializer _serializer;
      internal readonly string Address;

      public Runner(HandlerExecutionEngine handlerExecutionEngine, IMessageStorage storage, string address, RealEndpointConfiguration configuration, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
      {
         _handlerExecutionEngine = handlerExecutionEngine;
         _storage = storage;
         _configuration = configuration;

         _serverSocket = new RouterSocket();
         //Should we screw up with the pipelining we prefer performance problems (memory usage) to lost messages or blocking
         _serverSocket.Options.SendHighWatermark = int.MaxValue;
         _serverSocket.Options.ReceiveHighWatermark = int.MaxValue;

         //We guarantee delivery upon restart in other ways. When we shut down, just do it.
         _serverSocket.Options.Linger = 0.Milliseconds();

         Address = _serverSocket.BindAndReturnActualAddress(address);
         _serverSocket.ReceiveReady += HandleIncomingMessage_PollerThread;

         _responseQueue = new NetMQQueue<NetMQMessage>();

         _responseQueue.ReceiveReady += SendResponseMessage_PollerThread;

         _cancellationTokenSource = new CancellationTokenSource();
         _poller = new NetMQPoller {_serverSocket, _responseQueue};

         _typeMapper = typeMapper;
         _serializer = serializer;
         _messageReceiverThread = new Thread(ThreadExceptionHandler.WrapThreadStart(MessageReceiverThread)) {Name = $"{nameof(Inbox)}_{nameof(MessageReceiverThread)}_{_configuration.Name}"};
      }

      public async Task StartAsync()
      {
         _messageReceiverThread.Start();
         _handlerExecutionEngine.Start();
         await Task.CompletedTask.NoMarshalling();
         _poller.RunAsync($"{nameof(Inbox)}_PollerThread_{_configuration.Name}");

      }

      void MessageReceiverThread()
      {
         while(!_cancellationTokenSource.IsCancellationRequested)
         {
            var transportMessageBatch = _receivedMessageBatches.Take(_cancellationTokenSource.Token);
            foreach(var transportMessage in transportMessageBatch)
            {
               if(transportMessage.Is<IAtMostOnceMessage>())
               {
                  //bug: handle the exception that will be thrown if this is a duplicate message
                  _storage.SaveIncomingMessage(transportMessage);

                  if(transportMessage.Is<IExactlyOnceMessage>())
                  {
                     var persistedResponse = transportMessage.CreatePersistedResponse();
                     _responseQueue.Enqueue(persistedResponse);
                  }
               }

               var dispatchTask = _handlerExecutionEngine.Enqueue(transportMessage);

               //Bug: this returns a task that must be awaited somehow.
               dispatchTask.ContinueAsynchronouslyOnDefaultScheduler(dispatchResult =>
               {
                  //refactor: Consider moving these responsibilities into the message class or other class. Probably create more subtypes so that no type checking is required. See also: HandlerExecutionEngine.Coordinator and [.HandlerExecutionTask]
                  var message = transportMessage.DeserializeMessageAndCacheForNextCall();
                  if(message is IRequireAResponse)
                  {
                     if(dispatchResult.IsFaulted)
                     {
                        var failureResponse = transportMessage.CreateFailureResponse(Contract.ReturnNotNull(dispatchResult.Exception));
                        _responseQueue.Enqueue(failureResponse);
                     } else
                     {
                        Assert.Result.Assert(dispatchResult.IsCompleted);
                        try
                        {
                           if(message is IHasReturnValue<object>)
                           {
                              var successResponse = transportMessage.CreateSuccessResponseWithData(Contract.ReturnNotNull(dispatchResult.Result));
                              _responseQueue.Enqueue(successResponse);
                           } else
                           {
                              var successResponse = transportMessage.CreateSuccessResponse();
                              _responseQueue.Enqueue(successResponse);
                           }
                        }
                        catch(Exception exception)
                        {
                           var failureResponse = transportMessage.CreateFailureResponse(new AggregateException(exception));
                           _responseQueue.Enqueue(failureResponse);
                        }
                     }
                  }
               });
            }
         }
         // ReSharper disable once FunctionNeverReturns
      }

      void SendResponseMessage_PollerThread(object? sender, NetMQQueueEventArgs<NetMQMessage> e)
      {
         while(e.Queue.TryDequeue(out var response, TimeSpan.Zero))
         {
            _serverSocket.SendMultipartMessage(response.NotNull());
         }
      }

      void HandleIncomingMessage_PollerThread(object? sender, NetMQSocketEventArgs e)
      {
         Assert.Argument.Assert(e.IsReadyToReceive);
         var batch = TransportMessage.InComing.ReceiveBatch(_serverSocket, _typeMapper, _serializer);
         _receivedMessageBatches.Add(batch);
      }

      public void Dispose()
      {
         _cancellationTokenSource.Cancel();
         _cancellationTokenSource.Dispose();
         _messageReceiverThread.InterruptAndJoin();
         _poller.Stop();
         _poller.Dispose();
         _serverSocket.Close();
         _serverSocket.Dispose();
         _handlerExecutionEngine.Stop();

         _receivedMessageBatches.Dispose();
         _responseQueue.Dispose();
      }
   }
}