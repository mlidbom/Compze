using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Transport.Internal;
using Compze.Abstractions.Time;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Tessaging.Implementation.Transport.Client.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Implementation.Outbox;

class OutboxRetryPoller : IDisposable
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<OutboxRetryPoller>()
                                     .CreatedBy((Outbox.IMessageStorage messageStorage,
                                                 ITransportClient transportClient,
                                                 ITypeMapper typeMapper,
                                                 IRemotableMessageSerializer serializer,
                                                 ITaskRunner taskRunner,
                                                 IBackgroundExceptionReporter exceptionReporter)
                                                   => new OutboxRetryPoller(messageStorage, transportClient, typeMapper, serializer, taskRunner, exceptionReporter)));

   readonly Outbox.IMessageStorage _messageStorage;
   readonly ITransportClient _transportClient;
   readonly ITypeMapper _typeMapper;
   readonly IRemotableMessageSerializer _serializer;
   readonly ITaskRunner _taskRunner;
   readonly IBackgroundExceptionReporter _exceptionReporter;

   //Todo: implement a sane way of handling retries, something like an exponential backoff
   static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
   static readonly TimeSpan MessageAgeThatIsConsideredFailed = TimeSpan.FromSeconds(5);

   OutboxRetryPoller(Outbox.IMessageStorage messageStorage,
                     ITransportClient transportClient,
                     ITypeMapper typeMapper,
                     IRemotableMessageSerializer serializer,
                     ITaskRunner taskRunner,
                     IBackgroundExceptionReporter exceptionReporter)
   {
      _messageStorage = messageStorage;
      _transportClient = transportClient;
      _typeMapper = typeMapper;
      _serializer = serializer;
      _taskRunner = taskRunner;
      _exceptionReporter = exceptionReporter;
   }

   readonly CancellationTokenSource _cancellationTokenSource = new();
   Thread? _pollerThread;
   bool _running = false;

   public void Start()
   {
      Assert.State.Is(!_running);
      _running = true;
      _pollerThread = _taskRunner.RunOnNamedThread("OutboxRetryPoller", PollerLoop, ThreadPriority.BelowNormal);
   }

   internal void Stop()
   {
      if(_running)
      {
         _running = false;
         this.Log().Info("Stopping OutboxRetryPoller...");
         _cancellationTokenSource.Cancel();
         _pollerThread?.Join(TimeSpan.FromSeconds(5)); // Give it time to finish the current iteration
         _cancellationTokenSource.Dispose();
      }
   }

   public void Dispose() => Stop();

   void PollerLoop()
   {
      this.Log().Info("OutboxRetryPoller started");

      RetryUndeliveredMessages(TimeSpan.Zero);

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

   void RetryUndeliveredMessages(TimeSpan? minimumAge = null)
   {
      var undeliveredMessages = _messageStorage.GetUndeliveredMessages(minimumAge ?? MessageAgeThatIsConsideredFailed);
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
               var connections = _transportClient.SubscriberConnectionsFor(exactlyOnceEvent);
               connection = connections.FirstOrDefault(c => c.EndpointInformation.Id.GuidValue == endpointId)
                         ?? throw new InvalidOperationException($"No subscriber connection found for endpoint {endpointId}");
               sendTask = connection.SendAsync(exactlyOnceEvent);
               break;
            }
            case IExactlyOnceCommand exactlyOnceCommand:
            {
               connection = _transportClient.ConnectionToHandlerFor(exactlyOnceCommand);
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

   void HandleRetryResult(Task completedSendTask, Guid messageId, Guid endpointId) => _exceptionReporter.RunSwallowingAndReportingAnyExceptions(() =>
   {
      if(!_running)
         return; //We have shut down and storage may no longer be available/working. The recovery mechanisms will take care of this message after restart.
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

   void RecordFailure(Guid messageId, Guid endpointId, Exception? exception) =>
      _messageStorage.RecordDeliveryFailure(messageId, new EndpointId(endpointId), exception);
}
