using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compze.Abstractions.Internal;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Serialization;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Hosting.Implementation;

class OutboxRetryPoller : IDisposable
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<OutboxRetryPoller>()
                                     .CreatedBy((Outbox.IMessageStorage messageStorage,
                                                 ITransport transport,
                                                 ITypeMapper typeMapper,
                                                 IRemotableMessageSerializer serializer,
                                                 ITaskRunner taskRunner,
                                                 IBackgroundExceptionReporter exceptionReporter)
                                                   => new OutboxRetryPoller(messageStorage, transport, typeMapper, serializer, taskRunner, exceptionReporter)));

   readonly Outbox.IMessageStorage _messageStorage;
   readonly ITransport _transport;
   readonly ITypeMapper _typeMapper;
   readonly IRemotableMessageSerializer _serializer;
   readonly ITaskRunner _taskRunner;
   readonly IBackgroundExceptionReporter _exceptionReporter;

   readonly CancellationTokenSource _cancellationTokenSource = new();
   Thread? _pollerThread;

   // Exponential backoff configuration
   static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

   OutboxRetryPoller(Outbox.IMessageStorage messageStorage,
                     ITransport transport,
                     ITypeMapper typeMapper,
                     IRemotableMessageSerializer serializer,
                     ITaskRunner taskRunner,
                     IBackgroundExceptionReporter exceptionReporter)
   {
      _messageStorage = messageStorage;
      _transport = transport;
      _typeMapper = typeMapper;
      _serializer = serializer;
      _taskRunner = taskRunner;
      _exceptionReporter = exceptionReporter;
   }

   public void Start() => _pollerThread = _taskRunner.RunOnNamedThread("OutboxRetryPoller", PollerLoop, ThreadPriority.BelowNormal);

   void PollerLoop()
   {
      this.Log().Info("OutboxRetryPoller started");

      while(!_cancellationTokenSource.Token.IsCancellationRequested)
      {
         try
         {
            RetryUndeliveredMessages();
         }
         catch(Exception exception)
         {
            _exceptionReporter.ReportException(exception);
         }

         try
         {
            _cancellationTokenSource.Token.WaitHandle.WaitOne(PollingInterval);
         }
         catch(ObjectDisposedException)
         {
            // Expected during shutdown
            break;
         }
      }

      this.Log().Info("OutboxRetryPoller stopped");
   }

   void RetryUndeliveredMessages()
   {
      var undeliveredMessages = _messageStorage.GetUndeliveredMessages(TimeSpan.Zero);
      if(undeliveredMessages.Count == 0)
         return;

      this.Log().Info($"Found {undeliveredMessages.Count} undelivered message(s) to retry");
      undeliveredMessages.ForEach(RetryMessage);
   }

   void RetryMessage(IServiceBusSqlLayer.UndeliveredMessage undeliveredMessage)
   {
      var endpointId = undeliveredMessage.TargetEndpointId;

      try
      {
         var messageTypeId = new TypeId(undeliveredMessage.TypeIdGuid);
         var messageType = _typeMapper.GetType(messageTypeId);
         var message = _serializer.DeserializeMessage(messageType, undeliveredMessage.SerializedMessage);

         // Get the connection using the transport's routing logic
         IInboxConnection connection;
         Task sendTask;

         switch(message)
         {
            case IExactlyOnceEvent exactlyOnceEvent:
            {
               var connections = _transport.SubscriberConnectionsFor(exactlyOnceEvent);
               connection = connections.FirstOrDefault(c => c.EndpointInformation.Id.GuidValue == endpointId)
                         ?? throw new InvalidOperationException($"No subscriber connection found for endpoint {endpointId}");
               sendTask = connection.SendAsync(exactlyOnceEvent);
               break;
            }
            case IExactlyOnceCommand exactlyOnceCommand:
            {
               connection = _transport.ConnectionToHandlerFor(exactlyOnceCommand);
               if(connection.EndpointInformation.Id.GuidValue != endpointId)
               {
                  throw new InvalidOperationException($"Command routing changed - expected endpoint {endpointId}, got {connection.EndpointInformation.Id.GuidValue}");
               }

               sendTask = connection.SendAsync(exactlyOnceCommand);
               break;
            }
            default:
               throw new InvalidOperationException($"Unexpected message type: {message.GetType().FullName}");
         }

         this.Log().Debug($"Retrying delivery of message {undeliveredMessage.MessageId} to endpoint {endpointId} (attempt {undeliveredMessage.RetryCount + 1})");

         sendTask.ContinueAsynchronouslyOnDefaultScheduler(completedTask => HandleRetryResult(completedTask, undeliveredMessage.MessageId, endpointId));
      }
      catch(Exception exception)
      {
         this.Log().Error(exception, $"Error retrying message {undeliveredMessage.MessageId} to endpoint {endpointId}");
         RecordFailure(undeliveredMessage.MessageId, endpointId, exception);
      }
   }

   void HandleRetryResult(Task completedSendTask, Guid messageId, Guid endpointId)
   {
      _exceptionReporter.RunAndReportAnyExceptions(() =>
      {
         if(completedSendTask.IsFaulted)
         {
            if(completedSendTask.Exception != null)
            {
               this.Log().Warning(completedSendTask.Exception, $"Retry failed for message {messageId} to endpoint {endpointId}");
            } else
            {
               this.Log().Warning($"Retry failed for message {messageId} to endpoint {endpointId} - no exception details available");
            }

            RecordFailure(messageId, endpointId, completedSendTask.Exception);
         } else
         {
            this.Log().Info($"Successfully delivered message {messageId} to endpoint {endpointId}");
            _messageStorage.MarkAsReceived(messageId, new EndpointId(endpointId));
         }
      });
   }

   void RecordFailure(Guid messageId, Guid endpointId, Exception? exception) =>
      _messageStorage.RecordDeliveryFailure(messageId, new EndpointId(endpointId), exception);

   void Stop()
   {
      this.Log().Info("Stopping OutboxRetryPoller...");
      _cancellationTokenSource.Cancel();
      _pollerThread?.Join(TimeSpan.FromSeconds(5)); // Give it time to finish the current iteration
   }

   public void Dispose()
   {
      Stop();
      _cancellationTokenSource.Dispose();
   }
}
