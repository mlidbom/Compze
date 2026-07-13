using Compze.TypeIdentifiers;
using Compze.Abstractions.Public;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Hosting.Public;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Internals.Transport;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading.ResourceAccess;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;

class TessagingConnection(
   ITessagesInFlightTracker tessagesInFlightTracker,
   EndpointAddress remoteAddress,
   ITypeMap typeMap,
   IRemotableTessageSerializer serializer,
   ITransportMessagePoster transportMessagePoster,
   IInfrastructureQueryTransport infrastructureQueryTransport,
   Outbox.Outbox.ITessageStorage tessageStorage,
   ITaskRunner taskRunner,
   IBackgroundExceptionReporter exceptionReporter) : ITessagingInboxConnection, IDisposable
{
   public EndpointInformation EndpointInformation { get; private set; } = null!;

   readonly ITessagesInFlightTracker _tessagesInFlightTracker = tessagesInFlightTracker;
   readonly EndpointAddress _remoteAddress = remoteAddress;
   readonly ITypeMap _typeMap = typeMap;
   readonly IRemotableTessageSerializer _serializer = serializer;
   readonly ITransportMessagePoster _transportMessagePoster = transportMessagePoster;
   readonly IInfrastructureQueryTransport _infrastructureQueryTransport = infrastructureQueryTransport;
   readonly Outbox.Outbox.ITessageStorage _tessageStorage = tessageStorage;
   readonly ITaskRunner _taskRunner = taskRunner;
   readonly IBackgroundExceptionReporter _exceptionReporter = exceptionReporter;

   record PendingDelivery(TransportTessage.OutGoing TransportTessage);
   readonly IThreadShared<Queue<PendingDelivery>> _queue = IThreadShared.New(new Queue<PendingDelivery>());
   readonly AutoResetEvent _signal = new(false);
   readonly CancellationTokenSource _cancellationSource = new();
   Thread? _sendLoopThread;
   int _consecutiveFailures;
   bool _deliveryRunning;

   public async Task InitAsync()
   {
      EndpointInformation = await _infrastructureQueryTransport.GetAsync(new EndpointInformationQuery(), _remoteAddress).caf();
   }

   // Delivery management — enqueue for the send loop to process
   public void EnqueueForDelivery(ITessage tessage, TessageId dedupId)
   {
      var transportTessage = TransportTessage.OutGoing.Create(tessage, dedupId, _typeMap, _serializer);
      _tessagesInFlightTracker.SendingTessageOnTransport(transportTessage, EndpointInformation.Id);

      _queue.Locked(queue => queue.Enqueue(new PendingDelivery(transportTessage)));

      _signal.Set();
   }

   public void StartDelivery()
   {
      LoadUndeliveredTessages();
      _deliveryRunning = true;
      _sendLoopThread = _taskRunner.RunOnNamedThread($"DeliveryManager-{EndpointInformation.Id.Value:N}", SendLoop, ThreadPriority.BelowNormal);
   }

   //todo:bug The in-order delivery guarantee is LOST here on restart. The send loop preserves order while
   //running (single-threaded, head-of-line FIFO), but recovery re-enqueues the backlog in whatever order
   //GetUndeliveredTessagesForEndpoint returns it — and every backend orders it by RetryCount/LastAttemptTime
   //(retry metadata), NOT by original send order. A tessage stuck retrying at the head of a live queue
   //therefore comes back LAST after a restart, inverting the order. The outbox has inherited inbox-style
   //retry-ordering; an outbox must recover in send order. Needs a stable monotonic per-destination send-order
   //key. See src/TODO/TODO_bug-outbox-delivery-ordering-lost-on-recovery.md.
   void LoadUndeliveredTessages()
   {
      var undelivered = _tessageStorage.GetUndeliveredTessagesForEndpoint(EndpointInformation.Id);
      if(undelivered.Count == 0) return;

      this.Log().Info($"Loading {undelivered.Count} undelivered tessage(s) for recovery to endpoint {EndpointInformation.Id}");
      foreach(var undeliveredTessage in undelivered)
      {
         var tessageType = undeliveredTessage.TypeId.Type;
         var tessage = (ITessage)_serializer.DeserializeTessage(tessageType, undeliveredTessage.SerializedTessage);
         EnqueueForDelivery(tessage, undeliveredTessage.TessageId);
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
            var pending = _queue.Locked(queue => queue.Count > 0 ? queue.Peek() : null);

            if(pending == null)
            {
               WaitHandle.WaitAny([_signal, _cancellationSource.Token.WaitHandle]);
               continue;
            }

            if(TrySend(pending))
            {
               _queue.Locked(queue => queue.Dequeue());

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
         _transportMessagePoster.PostAsync(pending.TransportTessage, _remoteAddress).GetAwaiter().GetResult();

         this.Log().Debug($"Delivered tessage {pending.TransportTessage.TessageId} to endpoint {EndpointInformation.Id}");
         _exceptionReporter.RunSwallowingAndReportingAnyExceptions(() => _tessageStorage.MarkAsReceived(pending.TransportTessage.TessageId, EndpointInformation.Id));
         return true;
      }
#pragma warning disable CA1031 // Background thread — must catch all to keep the send loop running
      catch(Exception exception)
      {
#pragma warning restore CA1031
         this.Log().Warning(exception, $"Delivery failed for tessage {pending.TransportTessage.TessageId} to endpoint {EndpointInformation.Id}");
         _exceptionReporter.RunSwallowingAndReportingAnyExceptions(() => _tessageStorage.RecordDeliveryFailure(pending.TransportTessage.TessageId, EndpointInformation.Id, exception));
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
