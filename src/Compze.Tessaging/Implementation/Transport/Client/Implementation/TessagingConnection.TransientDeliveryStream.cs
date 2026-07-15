using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Internals.Logging;
using Compze.Threading.ResourceAccess;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation;

partial class TessagingConnection
{
   ///<summary>The connection's transient delivery stream: in-memory, best-effort, delivering in order while deliveries succeed —<br/>
   /// no store, no dedup, no retry (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>). Every connection carries this<br/>
   /// stream: it needs no machinery beyond the connection itself.</summary>
   class TransientDeliveryStream : IDisposable
   {
      readonly TessagingConnection _connection;
      readonly IThreadShared<Queue<TransportTessage.OutGoing>> _queue = IThreadShared.New(new Queue<TransportTessage.OutGoing>());
      readonly AutoResetEvent _signal = new(false);
      Thread? _sendLoopThread;

      internal TransientDeliveryStream(TessagingConnection connection) => _connection = connection;

      internal void Enqueue(TransportTessage.OutGoing transportTessage)
      {
         _queue.Locked(queue => queue.Enqueue(transportTessage));
         _signal.Set();
      }

      internal void Start() =>
         _sendLoopThread = _connection._taskRunner.RunOnNamedThread($"TransientDelivery-{_connection.EndpointInformation.Id.Value:N}", SendLoop, ThreadPriority.BelowNormal);

      internal void AwaitSendLoopTermination() => _sendLoopThread?.Join(5.Seconds());

      void SendLoop()
      {
         this.Log().Info($"Started transient delivery loop for endpoint {_connection.EndpointInformation.Id}");

         try
         {
            while(!_connection._cancellationSource.IsCancellationRequested)
            {
               //Dequeued before sending, never peeked: a transient tessage is attempted exactly once - a failure drops it (and the stream behind it), so nothing is ever re-sent.
               var pending = _queue.Locked(queue => queue.Count > 0 ? queue.Dequeue() : null);

               if(pending == null)
               {
                  WaitHandle.WaitAny([_signal, _connection._cancellationSource.Token.WaitHandle]);
                  continue;
               }

               try
               {
                  _connection._transportMessagePoster.PostAsync(pending, _connection._remoteAddress).GetAwaiter().GetResult();
                  this.Log().Debug($"Delivered transient tessage {pending.TessageId} to endpoint {_connection.EndpointInformation.Id}");
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

         this.Log().Info($"Stopped transient delivery loop for endpoint {_connection.EndpointInformation.Id}");
      }

      ///<summary>The transient tier's response to a delivery failure: the failed tessage and everything queued behind it are dropped<br/>
      /// together, so the subscriber's gap is one clean boundary — never a silent mid-stream skip that would deliver tessage 54 after<br/>
      /// dropping 53. Tessages enqueued after the drop form a new live stream, attempted normally: while the endpoint stays unreachable<br/>
      /// each attempt fails and drops whatever queued since, which is exactly what best-effort means.</summary>
      void DropTheQueuedTransientStreamWhole(TransportTessage.OutGoing failedTessage, Exception exception)
      {
         var droppedBehindFailed = _queue.Locked(queue =>
         {
            var queued = queue.ToArray();
            queue.Clear();
            return queued;
         });

         this.Log().Warning(exception, $"Transient delivery to endpoint {_connection.EndpointInformation.Id} failed: dropping the queued transient stream whole - the failed tessage {failedTessage.TessageId} plus {droppedBehindFailed.Length} tessage(s) queued behind it. The subscriber resumes from tessages published after this point.");

         _connection._tessagesInFlightTracker.DroppedBeforeDelivery(failedTessage, _connection.EndpointInformation.Id);
         foreach(var dropped in droppedBehindFailed)
            _connection._tessagesInFlightTracker.DroppedBeforeDelivery(dropped, _connection.EndpointInformation.Id);
      }

      public void Dispose() => _signal.Dispose();
   }
}
