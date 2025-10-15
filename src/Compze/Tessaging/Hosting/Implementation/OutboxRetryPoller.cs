using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compze.Abstractions.Internal;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Serialization;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Hosting.Implementation;

class OutboxRetryPoller : IDisposable
{
   internal static void RegisterWith(IDependencyRegistrar registrar)
      => registrar.Register(Singleton.For<OutboxRetryPoller>()
                                     .CreatedBy((IServiceBusSqlLayer.IOutboxSqlLayer sqlLayer,
                                                 ITransport transport,
                                                 ITypeMapper typeMapper,
                                                 IRemotableMessageSerializer serializer,
                                                 ITaskRunner taskRunner,
                                                 IBackgroundExceptionReporter exceptionReporter)
                                                   => new OutboxRetryPoller(sqlLayer, transport, typeMapper, serializer, taskRunner, exceptionReporter)));

   readonly IServiceBusSqlLayer.IOutboxSqlLayer _sqlLayer;
   readonly ITransport _transport;
   readonly ITypeMapper _typeMapper;
   readonly IRemotableMessageSerializer _serializer;
   readonly ITaskRunner _taskRunner;
   readonly IBackgroundExceptionReporter _exceptionReporter;

   readonly CancellationTokenSource _cancellationTokenSource = new();
   Thread? _pollerThread;

   // Exponential backoff configuration
   static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(10);
   static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);

   OutboxRetryPoller(IServiceBusSqlLayer.IOutboxSqlLayer sqlLayer,
                     ITransport transport,
                     ITypeMapper typeMapper,
                     IRemotableMessageSerializer serializer,
                     ITaskRunner taskRunner,
                     IBackgroundExceptionReporter exceptionReporter)
   {
      _sqlLayer = sqlLayer;
      _transport = transport;
      _typeMapper = typeMapper;
      _serializer = serializer;
      _taskRunner = taskRunner;
      _exceptionReporter = exceptionReporter;
   }

   public void Start()
   {
      _pollerThread = _taskRunner.RunOnNamedThread(
         threadName: "OutboxRetryPoller",
         start: PollerLoop,
         priority: ThreadPriority.BelowNormal);
   }

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
      var undeliveredMessages = _sqlLayer.GetUndeliveredMessages(InitialRetryDelay);

      if(undeliveredMessages.Count == 0) return;

      this.Log().Info($"Found {undeliveredMessages.Count} undelivered message(s) to retry");

      var messagesGroupedByEndpoint = undeliveredMessages
                                     .GroupBy(m => m.EndpointId)
                                     .ToDictionary(g => g.Key, g => g.ToList());

      foreach(var (endpointId, messages) in messagesGroupedByEndpoint)
      {
         foreach(var undeliveredMessage in messages)
         {
            RetryMessage(undeliveredMessage, endpointId);
         }
      }
   }

   void RetryMessage(IServiceBusSqlLayer.UndeliveredMessage undeliveredMessage, Guid endpointId)
   {
      try
      {
         var messageTypeId = new TypeId(undeliveredMessage.TypeIdGuidValue);
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

         TaskCE.ContinueAsynchronouslyOnDefaultScheduler(
            sendTask,
            completedTask => HandleRetryResult(completedTask, undeliveredMessage.MessageId, endpointId));
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
            var exception = completedSendTask.Exception?.GetBaseException();
            if(exception != null)
            {
               this.Log().Warning(exception, $"Retry failed for message {messageId} to endpoint {endpointId}");
            }

            RecordFailure(messageId, endpointId, exception);
         } else
         {
            this.Log().Info($"Successfully delivered message {messageId} to endpoint {endpointId}");
            _sqlLayer.MarkAsReceived(messageId, endpointId);
         }
      });
   }

   void RecordFailure(Guid messageId, Guid endpointId, Exception? exception)
   {
      var failureReason = exception != null
                             ? $"{exception.GetType().Name}: {exception.Message}"
                             : "Unknown failure";

      _sqlLayer.RecordDeliveryFailure(messageId, endpointId, failureReason);
   }

   public void Stop()
   {
      this.Log().Info("Stopping OutboxRetryPoller...");
      _cancellationTokenSource.Cancel();
      _pollerThread?.Join(TimeSpan.FromSeconds(5)); // Give it time to finish current iteration
   }

   public void Dispose()
   {
      Stop();
      _cancellationTokenSource.Dispose();
   }
}
