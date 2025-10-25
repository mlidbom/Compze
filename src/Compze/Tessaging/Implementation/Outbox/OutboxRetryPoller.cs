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
                                     .CreatedBy((Outbox.ITessageStorage tessageStorage,
                                                 ITransportClient transportClient,
                                                 ITypeMapper typeMapper,
                                                 IRemotableTessageSerializer serializer,
                                                 ITaskRunner taskRunner,
                                                 IBackgroundExceptionReporter exceptionReporter)
                                                   => new OutboxRetryPoller(tessageStorage, transportClient, typeMapper, serializer, taskRunner, exceptionReporter)));

   readonly Outbox.ITessageStorage _tessageStorage;
   readonly ITransportClient _transportClient;
   readonly ITypeMapper _typeMapper;
   readonly IRemotableTessageSerializer _serializer;
   readonly ITaskRunner _taskRunner;
   readonly IBackgroundExceptionReporter _exceptionReporter;

   //Todo: implement a sane way of handling retries, something like an exponential backoff
   static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
   static readonly TimeSpan TessageAgeThatIsConsideredFailed = TimeSpan.FromSeconds(5);

   OutboxRetryPoller(Outbox.ITessageStorage tessageStorage,
                     ITransportClient transportClient,
                     ITypeMapper typeMapper,
                     IRemotableTessageSerializer serializer,
                     ITaskRunner taskRunner,
                     IBackgroundExceptionReporter exceptionReporter)
   {
      _tessageStorage = tessageStorage;
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

      RetryUndeliveredTessages(TimeSpan.Zero);

      while(!_cancellationTokenSource.Token.IsCancellationRequested)
      {
         try
         {
            RetryUndeliveredTessages();
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

   void RetryUndeliveredTessages(TimeSpan? minimumAge = null)
   {
      var undeliveredTessages = _tessageStorage.GetUndeliveredTessages(minimumAge ?? TessageAgeThatIsConsideredFailed);
      if(undeliveredTessages.Count == 0)
         return;

      this.Log().Info($"Found {undeliveredTessages.Count} undelivered tessage(s) to retry");
      undeliveredTessages.ForEach(RetryTessage);
   }

   void RetryTessage(IServiceBusSqlLayer.UndeliveredTessage undeliveredTessage)
   {
      var endpointId = undeliveredTessage.TargetEndpointId;

      try
      {
         var tessageTypeId = new TypeId(undeliveredTessage.TypeIdGuid);
         var tessageType = _typeMapper.GetType(tessageTypeId);
         var tessage = _serializer.DeserializeTessage(tessageType, undeliveredTessage.SerializedTessage);

         // Get the connection using the transport's routing logic
         IInboxConnection connection;
         Task sendTask;

         switch(tessage)
         {
            case IExactlyOnceTevent exactlyOnceEvent:
            {
               var connections = _transportClient.SubscriberConnectionsFor(exactlyOnceEvent);
               connection = connections.FirstOrDefault(c => c.EndpointInformation.Id.GuidValue == endpointId)
                         ?? throw new InvalidOperationException($"No subscriber connection found for endpoint {endpointId}");
               sendTask = connection.SendAsync(exactlyOnceEvent);
               break;
            }
            case IExactlyOnceTommand exactlyOnceCommand:
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
               throw new InvalidOperationException($"Unexpected tessage type: {tessage.GetType().FullName}");
         }

         this.Log().Debug($"Retrying delivery of tessage {undeliveredTessage.TessageId} to endpoint {endpointId} (attempt {undeliveredTessage.RetryCount + 1})");

         sendTask.ContinueAsynchronouslyOnDefaultScheduler(completedTask => HandleRetryResult(completedTask, undeliveredTessage.TessageId, endpointId));
      }
      catch(Exception exception)
      {
         this.Log().Error(exception, $"Error retrying tessage {undeliveredTessage.TessageId} to endpoint {endpointId}");
         RecordFailure(undeliveredTessage.TessageId, endpointId, exception);
      }
   }

   void HandleRetryResult(Task completedSendTask, Guid tessageId, Guid endpointId) => _exceptionReporter.RunSwallowingAndReportingAnyExceptions(() =>
   {
      if(!_running)
         return; //We have shut down and storage may no longer be available/working. The recovery mechanisms will take care of this tessage after restart.
      if(completedSendTask.IsFaulted)
      {
         if(completedSendTask.Exception != null)
         {
            this.Log().Warning(completedSendTask.Exception, $"Retry failed for tessage {tessageId} to endpoint {endpointId}");
         } else
         {
            this.Log().Warning($"Retry failed for tessage {tessageId} to endpoint {endpointId} - no exception details available");
         }

         RecordFailure(tessageId, endpointId, completedSendTask.Exception);
      } else
      {
         this.Log().Info($"Successfully delivered tessage {tessageId} to endpoint {endpointId}");
         _tessageStorage.MarkAsReceived(tessageId, new EndpointId(endpointId));
      }
   });

   void RecordFailure(Guid tessageId, Guid endpointId, Exception? exception) =>
      _tessageStorage.RecordDeliveryFailure(tessageId, new EndpointId(endpointId), exception);
}
