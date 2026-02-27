using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Compze.Core.Public;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Outbox;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;

public class TessagingConnection : ITessagingInboxConnection, IDisposable
{
   public TessageTypesInternal.EndpointInformation EndpointInformation { get; private set; } = null!;
   IExactlyOnceTessageSender ExactlyOnceSender { get; set; } = null!;

   readonly ITessagesInFlightTracker _tessagesInFlightTracker;
   readonly EndPointAddress _remoteAddress;
   readonly ITypeMapper _typeMapper;
   readonly IRemotableTessageSerializer _serializer;
   readonly ITransportMessagePoster _transportMessagePoster;
   readonly Outbox.Outbox.ITessageStorage _tessageStorage;
   readonly ITaskRunner _taskRunner;
   readonly IBackgroundExceptionReporter _exceptionReporter;

   record PendingDelivery(TessageId TessageId, IExactlyOnceTessage Tessage);
   readonly object _queueLock = new();
   readonly Queue<PendingDelivery> _queue = new();
   readonly AutoResetEvent _signal = new(false);
   readonly CancellationTokenSource _cancellationSource = new();
   Thread? _sendLoopThread;
   int _consecutiveFailures;
   bool _deliveryRunning;

   public TessagingConnection(
      ITessagesInFlightTracker tessagesInFlightTracker,
      EndPointAddress remoteAddress,
      ITypeMapper typeMapper,
      IRemotableTessageSerializer serializer,
      ITransportMessagePoster transportMessagePoster,
      Outbox.Outbox.ITessageStorage tessageStorage,
      ITaskRunner taskRunner,
      IBackgroundExceptionReporter exceptionReporter)
   {
      _tessagesInFlightTracker = tessagesInFlightTracker;
      _remoteAddress = remoteAddress;
      _typeMapper = typeMapper;
      _serializer = serializer;
      _transportMessagePoster = transportMessagePoster;
      _tessageStorage = tessageStorage;
      _taskRunner = taskRunner;
      _exceptionReporter = exceptionReporter;
   }

   public async Task InitAsync()
   {
      var endpointInformationTuery = new TessageTypesInternal.EndpointInformationTuery();
      var endpointInformationTueryTessage = TransportTessage.OutGoing.Create(endpointInformationTuery, _typeMapper, _serializer);
      var endpointInformation = await _transportMessagePoster
                                     .PostAsync<TessageTypesInternal.EndpointInformation>(
                                         endpointInformationTueryTessage,
                                         endpointInformationTuery,
                                         _remoteAddress).caf();
      EndpointInformation = endpointInformation;
      ExactlyOnceSender = new HttpExactlyOnceTessageSender(_transportMessagePoster, _remoteAddress, _typeMapper, _serializer, _tessagesInFlightTracker, endpointInformation.Id);
   }

   // ITessagingInboxConnection — direct send (used internally by the delivery loop)
   public async Task SendAsync(IExactlyOnceTevent tevent) => await ExactlyOnceSender.SendAsync(tevent).caf();
   public async Task SendAsync(IExactlyOnceTommand tommand) => await ExactlyOnceSender.SendAsync(tommand).caf();

   // Delivery management
   public void Deliver(TessageId tessageId, IExactlyOnceTessage tessage)
   {
      var sendTask = tessage switch
      {
         IExactlyOnceTevent tevent => SendAsync(tevent),
         IExactlyOnceTommand tommand => SendAsync(tommand),
         _ => throw new InvalidOperationException($"Unexpected tessage type: {tessage.GetType().FullName}")
      };

      sendTask.ContinueWithCE(task =>
      {
         _exceptionReporter.RunSwallowingAndReportingAnyExceptions(() =>
         {
            if(task.IsFaulted)
            {
               this.Log().Warning(task.Exception!, $"Initial delivery failed for tessage {tessageId} to endpoint {EndpointInformation.Id}, enqueuing for retry");
               _tessageStorage.RecordDeliveryFailure(tessageId, EndpointInformation.Id, task.Exception);
               EnqueueForDelivery(tessageId, tessage);
            } else
            {
               this.Log().Debug($"Tessage {tessageId} delivered to endpoint {EndpointInformation.Id}");
               _tessageStorage.MarkAsReceived(tessageId, EndpointInformation.Id);
            }
         });
      });
   }

   public void EnqueueForDelivery(TessageId tessageId, IExactlyOnceTessage tessage)
   {
      lock(_queueLock) { _queue.Enqueue(new PendingDelivery(tessageId, tessage)); }

      _signal.Set();
   }

   public void StartDelivery()
   {
      LoadUndeliveredTessages();
      _deliveryRunning = true;
      _sendLoopThread = _taskRunner.RunOnNamedThread($"DeliveryManager-{EndpointInformation.Id.Value:N}", SendLoop, ThreadPriority.BelowNormal);
   }

   void LoadUndeliveredTessages()
   {
      var undelivered = _tessageStorage.GetUndeliveredTessagesForEndpoint(EndpointInformation.Id);
      if(undelivered.Count == 0) return;

      this.Log().Info($"Loading {undelivered.Count} undelivered tessage(s) for recovery to endpoint {EndpointInformation.Id}");
      foreach(var undeliveredTessage in undelivered)
      {
         var tessageType = _typeMapper.GetType(undeliveredTessage.TypeId);
         var tessage = (IExactlyOnceTessage)_serializer.DeserializeTessage(tessageType, undeliveredTessage.SerializedTessage);
         EnqueueForDelivery(undeliveredTessage.TessageId, tessage);
      }
   }

   public void StopDelivery()
   {
      if(_deliveryRunning)
      {
         _deliveryRunning = false;
         this.Log().Info($"Stopping delivery to endpoint {EndpointInformation.Id}...");
         _cancellationSource.Cancel();
         _sendLoopThread?.Join(5.Seconds());
      }
   }

   void SendLoop()
   {
      this.Log().Info($"Started delivery loop for endpoint {EndpointInformation.Id}");

      try
      {
         while(!_cancellationSource.IsCancellationRequested)
         {
            PendingDelivery? pending;
            lock(_queueLock)
            {
               pending = _queue.Count > 0 ? _queue.Peek() : null;
            }

            if(pending == null)
            {
               WaitHandle.WaitAny([_signal, _cancellationSource.Token.WaitHandle]);
               continue;
            }

            if(TrySend(pending))
            {
               lock(_queueLock) { _queue.Dequeue(); }

               _consecutiveFailures = 0;
            } else
            {
               _consecutiveFailures++;
               var backoff = TimeSpan.FromSeconds(0.5 * Math.Pow(2, Math.Min(_consecutiveFailures - 1, 7)));
               this.Log().Debug($"Backing off delivery to endpoint {EndpointInformation.Id} for {backoff.TotalSeconds:F1}s (consecutive failures: {_consecutiveFailures})");
               WaitHandle.WaitAny([_signal, _cancellationSource.Token.WaitHandle], backoff);
            }
         }
      }
      catch(ObjectDisposedException) {} // Expected during shutdown

      this.Log().Info($"Stopped delivery loop for endpoint {EndpointInformation.Id}");
   }

   bool TrySend(PendingDelivery pending)
   {
      try
      {
         switch(pending.Tessage)
         {
            case IExactlyOnceTevent tevent:
               SendAsync(tevent).GetAwaiter().GetResult();
               break;
            case IExactlyOnceTommand tommand:
               SendAsync(tommand).GetAwaiter().GetResult();
               break;
            default:
               throw new InvalidOperationException($"Unexpected tessage type: {pending.Tessage.GetType().FullName}");
         }

         this.Log().Debug($"Delivered tessage {pending.TessageId} to endpoint {EndpointInformation.Id}");
         _exceptionReporter.RunSwallowingAndReportingAnyExceptions(() => _tessageStorage.MarkAsReceived(pending.TessageId, EndpointInformation.Id));
         return true;
      }
#pragma warning disable CA1031 // Background thread — must catch all to keep the send loop running
      catch(Exception exception)
      {
#pragma warning restore CA1031
         this.Log().Warning(exception, $"Delivery failed for tessage {pending.TessageId} to endpoint {EndpointInformation.Id}");
         _exceptionReporter.RunSwallowingAndReportingAnyExceptions(() => _tessageStorage.RecordDeliveryFailure(pending.TessageId, EndpointInformation.Id, exception));
         return false;
      }
   }

   public void Dispose()
   {
      StopDelivery();
      _cancellationSource.Dispose();
      _signal.Dispose();
   }
}
