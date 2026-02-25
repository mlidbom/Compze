using Compze.Core.Public;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Core.Tessaging.Public;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Contracts;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE.LinqCE;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Implementation.Outbox;

public class OutboxRetryPoller : IDisposable
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<OutboxRetryPoller>()
                                     .CreatedBy((Outbox.ITessageStorage tessageStorage,
                                                 IRoutingInboxClient routingInboxClient,
                                                 ITypeMapper typeMapper,
                                                 IRemotableTessageSerializer serializer,
                                                 ITaskRunner taskRunner,
                                                 IBackgroundExceptionReporter exceptionReporter)
                                                   => new OutboxRetryPoller(tessageStorage, routingInboxClient, typeMapper, serializer, taskRunner, exceptionReporter)));

   readonly Outbox.ITessageStorage _tessageStorage;
   readonly IRoutingInboxClient _routingInboxClient;
   readonly ITypeMapper _typeMapper;
   readonly IRemotableTessageSerializer _serializer;
   readonly ITaskRunner _taskRunner;
   readonly IBackgroundExceptionReporter _exceptionReporter;

   //Todo: implement a sane way of handling retries, something like an exponential backoff
   static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
   static readonly TimeSpan TessageAgeThatIsConsideredFailed = TimeSpan.FromSeconds(5);

   OutboxRetryPoller(Outbox.ITessageStorage tessageStorage,
                     IRoutingInboxClient routingInboxClient,
                     ITypeMapper typeMapper,
                     IRemotableTessageSerializer serializer,
                     ITaskRunner taskRunner,
                     IBackgroundExceptionReporter exceptionReporter)
   {
      _tessageStorage = tessageStorage;
      _routingInboxClient = routingInboxClient;
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
      ContractAssertion.State.Is(!_running);
      _running = true;
      _pollerThread = _taskRunner.RunOnNamedThread("OutboxRetryPoller", PollerLoop, ThreadPriority.BelowNormal);
   }

   public void Stop()
   {
      if(_running)
      {
         _running = false;
         this.Log().Info("Stopping OutboxRetryPoller...");
         _cancellationTokenSource.Cancel();
         _pollerThread!.Join(TimeSpan.FromSeconds(5)); // Give it time to finish the current iteration
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
#pragma warning disable CA1031 //This is specifically designed for making sure that exceptions thrown in places where they cannot be surfaced directly, are not just ignored
         catch(Exception exception)
         {
#pragma warning restore CA1031
            //todo: should we really be reporting as real exceptions every message delivery failure running in a loop?
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
         var tessageTypeId = undeliveredTessage.TypeId;
         var tessageType = _typeMapper.GetType(tessageTypeId);
         var tessage = _serializer.DeserializeTessage(tessageType, undeliveredTessage.SerializedTessage);

         // Get the connection using the transport's routing logic
         IInboxConnection connection;
         Task sendTask;

         switch(tessage)
         {
            case IExactlyOnceTevent exactlyOnceTevent:
            {
               var connections = _routingInboxClient.SubscriberConnectionsFor(exactlyOnceTevent);
               connection = connections.FirstOrDefault(c => c.EndpointInformation.Id == endpointId)
                         ?? throw new InvalidOperationException($"No subscriber connection found for endpoint {endpointId}");
               sendTask = connection.SendAsync(exactlyOnceTevent);
               break;
            }
            case IExactlyOnceTommand exactlyOnceTommand:
            {
               connection = _routingInboxClient.ConnectionToHandlerFor(exactlyOnceTommand);
               if(connection.EndpointInformation.Id != endpointId)
               {
                  throw new InvalidOperationException($"Tommand routing changed - expected endpoint {endpointId}, got {connection.EndpointInformation.Id}");
               }

               sendTask = connection.SendAsync(exactlyOnceTommand);
               break;
            }
            default:
               throw new InvalidOperationException($"Unexpected tessage type: {tessage.GetType().FullName}");
         }

         this.Log().Debug($"Retrying delivery of tessage {undeliveredTessage.TessageId} to endpoint {endpointId} (attempt {undeliveredTessage.RetryCount + 1})");

         sendTask.ContinueWithCE(completedTask => HandleRetryResult(completedTask, undeliveredTessage.TessageId, endpointId));
      }
#pragma warning disable CA1031 //todo: we cannot throw here on a background thread, but maybe we should be checking what the failures are more specifically and pass some on to the exception reporter?
      catch(Exception exception)
      {
#pragma warning restore CA1031
         this.Log().Error(exception, $"Error retrying tessage {undeliveredTessage.TessageId} to endpoint {endpointId}");
         RecordFailure(undeliveredTessage.TessageId, endpointId, exception);
      }
   }

   void HandleRetryResult(Task completedSendTask, TessageId tessageId, EndpointId endpointId) => _exceptionReporter.RunSwallowingAndReportingAnyExceptions(() =>
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
         _tessageStorage.MarkAsReceived(tessageId, endpointId);
      }
   });

   void RecordFailure(TessageId tessageId, EndpointId endpointId, Exception? exception) =>
      _tessageStorage.RecordDeliveryFailure(tessageId, endpointId, exception);
}
