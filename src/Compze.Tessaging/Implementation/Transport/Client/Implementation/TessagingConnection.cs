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

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation;

class TessagingConnection(
   ITessagesInFlightTracker tessagesInFlightTracker,
   EndpointAddress remoteAddress,
   ITypeMap typeMap,
   ITessagingSerializer serializer,
   ITransportMessagePoster transportMessagePoster,
   IEndpointDiscoveryQueryTransport endpointDiscoveryQueryTransport,
   Outbox.Outbox.ITessageStorage tessageStorage,
   ITaskRunner taskRunner,
   IBackgroundExceptionReporter exceptionReporter) : ITessagingInboxConnection, IDisposable
{
   public EndpointInformation EndpointInformation { get; private set; } = null!;

   ///<summary>The address this connection delivers to — the endpoint's location when the connection was made; the endpoint's identity is <see cref="EndpointInformation"/>'s id.</summary>
   internal EndpointAddress RemoteAddress => _remoteAddress;

   readonly ITessagesInFlightTracker _tessagesInFlightTracker = tessagesInFlightTracker;
   readonly EndpointAddress _remoteAddress = remoteAddress;
   readonly ITypeMap _typeMap = typeMap;
   readonly ITessagingSerializer _serializer = serializer;
   readonly ITransportMessagePoster _transportMessagePoster = transportMessagePoster;
   readonly IEndpointDiscoveryQueryTransport _endpointDiscoveryQueryTransport = endpointDiscoveryQueryTransport;
   readonly Outbox.Outbox.ITessageStorage _tessageStorage = tessageStorage;
   readonly ITaskRunner _taskRunner = taskRunner;
   readonly IBackgroundExceptionReporter _exceptionReporter = exceptionReporter;

   readonly IThreadShared<Queue<TransportTessage.OutGoing>> _exactlyOnceQueue = IThreadShared.New(new Queue<TransportTessage.OutGoing>());
   readonly AutoResetEvent _exactlyOnceSignal = new(false);
   readonly IThreadShared<Queue<TransportTessage.OutGoing>> _transientQueue = IThreadShared.New(new Queue<TransportTessage.OutGoing>());
   readonly AutoResetEvent _transientSignal = new(false);
   readonly CancellationTokenSource _cancellationSource = new();
   Thread? _exactlyOnceSendLoopThread;
   Thread? _transientSendLoopThread;
   int _consecutiveFailures;
   bool _deliveryRunning;

   public async Task InitAsync() =>
      EndpointInformation = await _endpointDiscoveryQueryTransport.GetAsync(new EndpointInformationQuery(), _remoteAddress).caf();

   public void EnqueueForExactlyOnceDelivery(ITessage tessage, TessageId dedupId)
   {
      var transportTessage = TransportTessage.OutGoing.Create(tessage, dedupId, _typeMap, _serializer);
      _tessagesInFlightTracker.SendingTessageOnTransport(transportTessage, EndpointInformation.Id);

      _exactlyOnceQueue.Locked(queue => queue.Enqueue(transportTessage));

      _exactlyOnceSignal.Set();
   }

   public void EnqueueForTransientDelivery(ITessage tessage, TessageId envelopeId)
   {
      var transportTessage = TransportTessage.OutGoing.Create(tessage, envelopeId, _typeMap, _serializer);
      _tessagesInFlightTracker.SendingTessageOnTransport(transportTessage, EndpointInformation.Id);

      _transientQueue.Locked(queue => queue.Enqueue(transportTessage));

      _transientSignal.Set();
   }

   public void StartDelivery()
   {
      LoadUndeliveredTessages();
      _deliveryRunning = true;
      _exactlyOnceSendLoopThread = _taskRunner.RunOnNamedThread($"ExactlyOnceDelivery-{EndpointInformation.Id.Value:N}", ExactlyOnceSendLoop, ThreadPriority.BelowNormal);
      _transientSendLoopThread = _taskRunner.RunOnNamedThread($"TransientDelivery-{EndpointInformation.Id.Value:N}", TransientSendLoop, ThreadPriority.BelowNormal);
   }

   //In-order delivery survives recovery: GetUndeliveredTessagesForEndpoint returns the backlog in send order
   //(the outbox tessage table's monotonic GeneratedId), so re-enqueueing it re-establishes head-of-line on the
   //oldest undelivered tessage - the same order the send loop preserves while running.
   void LoadUndeliveredTessages()
   {
      var undelivered = _tessageStorage.GetUndeliveredTessagesForEndpoint(EndpointInformation.Id);
      if(undelivered.Count == 0) return;

      this.Log().Info($"Loading {undelivered.Count} undelivered tessage(s) for recovery to endpoint {EndpointInformation.Id}");
      foreach(var undeliveredTessage in undelivered)
      {
         var tessageType = undeliveredTessage.TypeId.Type;
         var tessage = _serializer.DeserializeTessage(tessageType, undeliveredTessage.SerializedTessage);
         EnqueueForExactlyOnceDelivery(tessage, undeliveredTessage.TessageId);
      }
   }

   public void StopDelivery()
   {
      if(!_deliveryRunning) return;

      _deliveryRunning = false;
      this.Log().Info($"Stopping delivery to endpoint {EndpointInformation.Id}...");
      _cancellationSource.Cancel();
      _exactlyOnceSendLoopThread?.Join(5.Seconds());
      _transientSendLoopThread?.Join(5.Seconds());
   }

   void ExactlyOnceSendLoop()
   {
      this.Log().Info($"Started exactly-once delivery loop for endpoint {EndpointInformation.Id}");

      try
      {
         while(!_cancellationSource.IsCancellationRequested)
         {
            var pending = _exactlyOnceQueue.Locked(queue => queue.Count > 0 ? queue.Peek() : null);

            if(pending == null)
            {
               WaitHandle.WaitAny([_exactlyOnceSignal, _cancellationSource.Token.WaitHandle]);
               continue;
            }

            if(TrySendExactlyOnce(pending))
            {
               _exactlyOnceQueue.Locked(queue => queue.Dequeue());

               _consecutiveFailures = 0;
            } else
            {
               _consecutiveFailures++;
               var backoff = TimeSpan.FromSeconds(0.5 * Math.Pow(2, Math.Min(_consecutiveFailures - 1, 7)));
               this.Log().Debug($"Backing off exactly-once delivery to endpoint {EndpointInformation.Id} for {backoff.TotalSeconds:F1}s (consecutive failures: {_consecutiveFailures})");
               WaitHandle.WaitAny([_exactlyOnceSignal, _cancellationSource.Token.WaitHandle], backoff);
            }
         }
      }
      catch(ObjectDisposedException) {} // Expected during shutdown

      this.Log().Info($"Stopped exactly-once delivery loop for endpoint {EndpointInformation.Id}");
   }

   bool TrySendExactlyOnce(TransportTessage.OutGoing pending)
   {
      try
      {
         _transportMessagePoster.PostAsync(pending, _remoteAddress).GetAwaiter().GetResult();

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

   void TransientSendLoop()
   {
      this.Log().Info($"Started transient delivery loop for endpoint {EndpointInformation.Id}");

      try
      {
         while(!_cancellationSource.IsCancellationRequested)
         {
            //Dequeued before sending, never peeked: a transient tessage is attempted exactly once - a failure drops it (and the stream behind it), so nothing is ever re-sent.
            var pending = _transientQueue.Locked(queue => queue.Count > 0 ? queue.Dequeue() : null);

            if(pending == null)
            {
               WaitHandle.WaitAny([_transientSignal, _cancellationSource.Token.WaitHandle]);
               continue;
            }

            try
            {
               _transportMessagePoster.PostAsync(pending, _remoteAddress).GetAwaiter().GetResult();
               this.Log().Debug($"Delivered transient tessage {pending.TessageId} to endpoint {EndpointInformation.Id}");
            }
#pragma warning disable CA1031 // Background thread, and the drop-stream-whole policy below IS the transient tier's handling of every delivery failure.
            catch(Exception exception)
            {
#pragma warning restore CA1031
               DropTheQueuedTransientStreamWhole(failedTessage: pending, exception);
            }
         }
      }
      catch(ObjectDisposedException) {} // Expected during shutdown

      this.Log().Info($"Stopped transient delivery loop for endpoint {EndpointInformation.Id}");
   }

   ///<summary>The transient tier's response to a delivery failure: the failed tessage and everything queued behind it are dropped<br/>
   /// together, so the subscriber's gap is one clean boundary — never a silent mid-stream skip that would deliver tessage 54 after<br/>
   /// dropping 53. Tessages enqueued after the drop form a new live stream, attempted normally: while the endpoint stays unreachable<br/>
   /// each attempt fails and drops whatever queued since, which is exactly what best-effort means.</summary>
   void DropTheQueuedTransientStreamWhole(TransportTessage.OutGoing failedTessage, Exception exception)
   {
      var droppedBehindFailed = _transientQueue.Locked(queue =>
      {
         var queued = queue.ToArray();
         queue.Clear();
         return queued;
      });

      this.Log().Warning(exception, $"Transient delivery to endpoint {EndpointInformation.Id} failed: dropping the queued transient stream whole - the failed tessage {failedTessage.TessageId} plus {droppedBehindFailed.Length} tessage(s) queued behind it. The subscriber resumes from tessages published after this point.");

      _tessagesInFlightTracker.DroppedBeforeDelivery(failedTessage, EndpointInformation.Id);
      foreach(var dropped in droppedBehindFailed)
         _tessagesInFlightTracker.DroppedBeforeDelivery(dropped, EndpointInformation.Id);
   }

   public void Dispose()
   {
      StopDelivery();
      _cancellationSource.Dispose();
      _exactlyOnceSignal.Dispose();
      _transientSignal.Dispose();
   }
}
