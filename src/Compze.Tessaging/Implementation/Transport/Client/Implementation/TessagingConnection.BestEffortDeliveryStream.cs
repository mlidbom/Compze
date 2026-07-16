using Compze.Tessaging.Implementation.BestEffortDelivery;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Internals.Logging;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation;

partial class TessagingConnection
{
   ///<summary>The connection's best-effort delivery stream: the pump that drains the peer's in-memory queue<br/>
   /// (<see cref="BestEffortTeventQueues.PeerQueue"/>, which outlives the connection) in publish order while the connection is<br/>
   /// live. A delivery failure pauses the stream whole: the one tessage in flight at the failure is dropped, loudly — without<br/>
   /// receiver dedup a re-send could duplicate it — while everything queued behind it stays queued in order, and draining resumes<br/>
   /// once the peer answers a tessage-free probe (or a new connection to the returned peer drains the same queue). The tier's<br/>
   /// loss surface is exactly: that single in-flight tessage, a crash of this process (memory is memory), and queue overflow<br/>
   /// (see <c>dev_docs/TODO/durable-peer-topology.md</c>).</summary>
   internal class BestEffortDeliveryStream
   {
      ///<summary>Creates the <see cref="BestEffortDeliveryStream"/> each connection carries, binding it to the peer's queue in<br/>
      /// <see cref="BestEffortTeventQueues"/> — registered by the best-effort delivery wiring, which owns the queues: the same<br/>
      /// wiring-supplies-the-streams idiom as the exactly-once stream factory the outbox registers.</summary>
      internal class Factory
      {
         readonly BestEffortTeventQueues _queues;

         internal Factory(BestEffortTeventQueues queues) => _queues = queues;

         internal BestEffortDeliveryStream CreateFor(TessagingConnection connection) => new(connection, _queues);
      }

      readonly TessagingConnection _connection;
      readonly BestEffortTeventQueues _queues;
      BestEffortTeventQueues.PeerQueue _peerQueue = null!;
      Thread? _sendLoopThread;
      int _consecutiveProbeFailures;

      BestEffortDeliveryStream(TessagingConnection connection, BestEffortTeventQueues queues)
      {
         _connection = connection;
         _queues = queues;
      }

      internal void Start()
      {
         //Resolved here rather than at construction: the peer's identity arrives with the connection's InitAsync.
         _peerQueue = _queues.For(_connection.EndpointInformation.Id);
         _sendLoopThread = _connection._taskRunner.RunOnNamedThread($"BestEffortDelivery-{_connection.EndpointInformation.Id.Value:N}", SendLoop, ThreadPriority.BelowNormal);
      }

      internal void AwaitSendLoopTermination() => _sendLoopThread?.Join(5.Seconds());

      void SendLoop()
      {
         this.Log().Info($"Started best-effort delivery loop for endpoint {_connection.EndpointInformation.Id}");

         try
         {
            while(!_connection._cancellationSource.IsCancellationRequested)
            {
               //Dequeued before sending, never peeked: nothing on this tier is ever re-sent - there is no receiver dedup to
               //make a re-send safe - so a tessage whose delivery attempt fails is gone rather than risked twice.
               var pending = _peerQueue.TryDequeue();

               if(pending == null)
               {
                  WaitHandle.WaitAny([_peerQueue.EnqueuedSignal, _connection._cancellationSource.Token.WaitHandle]);
                  continue;
               }

               try
               {
                  _connection._transportMessagePoster.PostAsync(pending, _connection.RemoteAddress).GetAwaiter().GetResult();
                  this.Log().Debug($"Delivered best-effort tessage {pending.TessageId} to endpoint {_connection.EndpointInformation.Id}");
               }
#pragma warning disable CA1031 // Background thread, and pausing the stream below IS the best-effort tier's handling of every delivery failure.
               catch(Exception exception)
               {
#pragma warning restore CA1031
                  DropTheInFlightTessageAndPauseUntilThePeerAnswersAProbe(inFlightAtFailure: pending, exception);
               }
            }
         }
         catch(ObjectDisposedException) {} // Expected during shutdown

         this.Log().Info($"Stopped best-effort delivery loop for endpoint {_connection.EndpointInformation.Id}");
      }

      ///<summary>Pause-stream-whole: the failed tessage was in flight at the failure moment — the one intrinsically ambiguous<br/>
      /// loss, dropped loudly — and the remainder stays queued in order while delivery pauses. Draining resumes only when the<br/>
      /// peer answers the endpoint-information query: a tessage-free probe, so waiting out an outage never spends real tessages<br/>
      /// finding out whether the peer is back. A peer that returns at a new address resumes through the router instead — the<br/>
      /// replacement connection's stream drains this same queue, and this paused loop dies with its dropped connection.</summary>
      void DropTheInFlightTessageAndPauseUntilThePeerAnswersAProbe(TransportTessage.OutGoing inFlightAtFailure, Exception exception)
      {
         this.Log().Warning(exception, $"Best-effort delivery to endpoint {_connection.EndpointInformation.Id} failed: the in-flight tessage {inFlightAtFailure.TessageId} is dropped - without receiver dedup a re-send could duplicate it - and the stream pauses with the remainder queued, resuming when the peer is reachable again.");
         _connection._tessagesInFlightTracker.DroppedBeforeDelivery(inFlightAtFailure, _connection.EndpointInformation.Id);

         _consecutiveProbeFailures = 0;
         while(!_connection._cancellationSource.IsCancellationRequested)
         {
            var backoff = TimeSpan.FromSeconds(0.5 * Math.Pow(2, Math.Min(_consecutiveProbeFailures, 7)));
            WaitHandle.WaitAny([_connection._cancellationSource.Token.WaitHandle], backoff);
            if(_connection._cancellationSource.IsCancellationRequested) return;

            try
            {
               _connection._endpointDiscoveryQueryTransport.GetAsync(new TessagingEndpointInformationQuery(), _connection.RemoteAddress).GetAwaiter().GetResult();
               this.Log().Info($"Endpoint {_connection.EndpointInformation.Id} answered the probe; best-effort delivery resumes with the queued remainder.");
               return;
            }
#pragma warning disable CA1031 // The probe failing means the peer is still unreachable - exactly what is being waited out.
            catch(Exception probeException)
            {
#pragma warning restore CA1031
               _consecutiveProbeFailures++;
               this.Log().Debug($"Probe of endpoint {_connection.EndpointInformation.Id} failed ({probeException.GetType().Name}); best-effort delivery stays paused (consecutive probe failures: {_consecutiveProbeFailures}).");
            }
         }
      }
   }
}
