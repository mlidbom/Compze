using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE;
using Compze.Tessaging.Private.SystemCE.ThreadingCE;
using Compze.Tessaging.Internal.Transport;
using Compze.Tessaging.TessageBus.Private.Outbox;
using Compze.Threading.ResourceAccess;
using Compze.Tessaging.Private.Transport;

namespace Compze.Tessaging.Private.Routing;

partial class TessagingConnection
{
   ///<summary>The connection's exactly-once delivery stream: the durable, head-of-line send queue backed by the outbox's storage.<br/>
   /// The loop does not look at the next tessage until the current one is delivered and acknowledged, retrying a failed delivery<br/>
   /// with backoff until the subscriber acknowledges it; the storage bookkeeping (delivered / failed) and the recovery at start —<br/>
   /// reloading the endpoint's undelivered backlog in send order — are what make the guarantee and the ordering survive restarts<br/>
   /// (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>). A connection carries this stream exactly when the endpoint<br/>
   /// wires the outbox, whose registration grants the router the storage.</summary>
   internal class ExactlyOnceDeliveryStream : IDisposable
   {
      ///<summary>Creates the <see cref="ExactlyOnceDeliveryStream"/> each connection carries. Registered as a component-set member<br/>
      /// by the outbox's wiring, which owns the storage backing every stream: the set's emptiness is what tells the router that the<br/>
      /// endpoint wires no exactly-once delivery, so its connections carry no such stream — the same wiring-supplies-the-legs idiom<br/>
      /// as the <c>IExactlyOnceTeventDeliveryLeg</c> set the <c>IUnitOfWorkTeventPublisher</c> routes through.</summary>
      internal class Factory
      {
         readonly Outbox.ITessageStorage _tessageStorage;

         internal Factory(Outbox.ITessageStorage tessageStorage) => _tessageStorage = tessageStorage;

         internal ExactlyOnceDeliveryStream CreateFor(TessagingConnection connection) => new(connection, _tessageStorage);
      }

      readonly TessagingConnection _connection;
      readonly Outbox.ITessageStorage _tessageStorage;
      readonly IThreadShared<Queue<TransportTessage.OutGoing>> _queue = IThreadShared.New(new Queue<TransportTessage.OutGoing>());
      readonly AutoResetEvent _signal = new(false);
      Thread? _sendLoopThread;
      int _consecutiveFailures;

      internal ExactlyOnceDeliveryStream(TessagingConnection connection, Outbox.ITessageStorage tessageStorage)
      {
         _connection = connection;
         _tessageStorage = tessageStorage;
      }

      internal void Enqueue(TransportTessage.OutGoing transportTessage)
      {
         _queue.Locked(queue => queue.Enqueue(transportTessage));
         _signal.Set();
      }

      internal void Start()
      {
         LoadUndeliveredTessages();
         _sendLoopThread = _connection._taskRunner.RunOnNamedThread($"ExactlyOnceDelivery-{_connection.EndpointInformation.Id.Value:N}", SendLoop, ThreadPriority.BelowNormal);
      }

      internal void AwaitSendLoopTermination() => _sendLoopThread?.JoinCE(5.Seconds());

      //In-order delivery survives recovery: GetUndeliveredTessagesForEndpoint returns the backlog in send order
      //(the outbox tessage table's monotonic GeneratedId), so re-enqueueing it re-establishes head-of-line on the
      //oldest undelivered tessage - the same order the send loop preserves while running.
      void LoadUndeliveredTessages()
      {
         //The stream is a dedicated-thread head-of-line pump, deliberately synchronous - it bridges its storage's async calls
         //the same way it already bridges the transport post below.
         var undelivered = _tessageStorage.GetUndeliveredTessagesForEndpointAsync(_connection.EndpointInformation.Id).GetAwaiter().GetResult();
         if(undelivered.Count == 0) return;

         this.Log().Info($"Loading {undelivered.Count} undelivered tessage(s) for recovery to endpoint {_connection.EndpointInformation.Id}");
         foreach(var undeliveredTessage in undelivered)
         {
            var tessageType = undeliveredTessage.TypeId.Type;
            var tessage = _connection._serializer.DeserializeTessage(tessageType, undeliveredTessage.SerializedTessage);
            _connection.EnqueueForExactlyOnceDelivery(tessage, undeliveredTessage.TessageId);
         }
      }

      void SendLoop()
      {
         this.Log().Info($"Started exactly-once delivery loop for endpoint {_connection.EndpointInformation.Id}");

         try
         {
            while(!_connection._cancellationSource.IsCancellationRequested)
            {
               var pending = _queue.Locked(queue => queue.Count > 0 ? queue.Peek() : null);

               if(pending == null)
               {
                  WaitHandle.WaitAny([_signal, _connection._cancellationSource.Token.WaitHandle]);
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
                  this.Log().Debug($"Backing off exactly-once delivery to endpoint {_connection.EndpointInformation.Id} for {backoff.TotalSeconds:F1}s (consecutive failures: {_consecutiveFailures})");
                  WaitHandle.WaitAny([_signal, _connection._cancellationSource.Token.WaitHandle], backoff);
               }
            }
         }
         catch(OperationCanceledException) {} // Expected during shutdown: cancellation aborted an in-flight send.
         catch(ObjectDisposedException) {} // Expected during shutdown

         this.Log().Info($"Stopped exactly-once delivery loop for endpoint {_connection.EndpointInformation.Id}");
      }

      bool TrySend(TransportTessage.OutGoing pending)
      {
         try
         {
            _connection._transportMessagePoster.PostAsync(pending, _connection.RemoteAddress, _connection._cancellationSource.Token).GetAwaiter().GetResult();

            this.Log().Debug($"Delivered tessage {pending.TessageId} to endpoint {_connection.EndpointInformation.Id}");
            _connection._exceptionReporter.RunSwallowingAndReportingAnyExceptions(() => _tessageStorage.MarkAsReceivedAsync(pending.TessageId, _connection.EndpointInformation.Id).GetAwaiter().GetResult());
            return true;
         }
#pragma warning disable CA1031 // Background thread — must catch all to keep the send loop running
         catch(Exception exception)
         {
#pragma warning restore CA1031
            //Shutdown cancelled the in-flight send: not a delivery failure. The tessage stays undelivered in the outbox and
            //redelivers on the next start (deduplicated by the receiver), so let the cancellation propagate and end the loop.
            _connection._cancellationSource.Token.ThrowIfCancellationRequested();
            this.Log().Warning(exception, $"Delivery failed for tessage {pending.TessageId} to endpoint {_connection.EndpointInformation.Id}");
            _connection._exceptionReporter.RunSwallowingAndReportingAnyExceptions(() => _tessageStorage.RecordDeliveryFailureAsync(pending.TessageId, _connection.EndpointInformation.Id, exception).GetAwaiter().GetResult());
            return false;
         }
      }

      public void Dispose() => _signal.Dispose();
   }
}
