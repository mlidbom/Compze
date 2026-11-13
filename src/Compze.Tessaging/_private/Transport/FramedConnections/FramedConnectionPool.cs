using Compze.Internals.SystemCE.ThreadingCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.Typermedia;
using Compze.Threading;
using Compze.Tessaging._internal.Transport;
using Compze.Tessaging._private.Transport.NamedPipes;

namespace Compze.Tessaging._private.Transport.FramedConnections;

///<summary>A client's bounded pool of persistent connections to one peer's <see cref="FramedConnectionServer"/>.<br/>
/// <see cref="ExchangeAsync"/> performs one exchange — one request frame answered by one response frame, the wire format's<br/>
/// lockstep unit — on a connection leased exclusively for that exchange and then returned for the next sender, so a burst of<br/>
/// concurrent sends reuses a handful of standing connections instead of opening a connection per send.</summary>
///<remarks>Connections are opened by the transport's <see cref="IConnectionOpener"/>, on demand, and at most<br/>
/// <see cref="MaximumConcurrentExchanges"/> at once. That bound is what makes the named-pipe transport's connect safe:<br/>
/// it holds a thread-pool thread, and unbounded it starved the process (<see cref="NamedPipeConnectionOpener"/> tells the<br/>
/// whole story). With the pool, a cold burst costs at most <see cref="MaximumConcurrentExchanges"/> brief connects, ever,<br/>
/// and steady-state sends never connect at all. The server side needs nothing special: <see cref="FramedConnectionServer"/><br/>
/// serves each connection for as long as the client keeps it open.</remarks>
///<remarks>No connection is ever handed out: the lease lives entirely inside <see cref="ExchangeAsync"/>, whose signature is<br/>
/// tessage in, response out — there is nothing a caller could leak or forget to release. Only the wire exchange runs inside<br/>
/// a lease, so an exception inside one means the connection's lockstep state is unknown — a half-written request, an<br/>
/// unconsumed response that would answer the NEXT sender's request — and the connection is discarded, never returned:<br/>
/// a pooled connection is always idle between complete exchanges, by construction. A completed exchange whose response<br/>
/// reports a handler exception is not a wire failure: the connection is clean and returns to the pool.</remarks>
class FramedConnectionPool : IDisposable
{
   ///<summary>Bounds both the standing connections to the peer and the exchanges in flight to it at once — a lease holds its<br/>
   /// connection for the whole round trip, including the serving endpoint's handler execution. Senders beyond the bound<br/>
   /// await a free connection without holding a thread: the pool is the transport's backpressure.</summary>
   ///<remarks>Measured over named pipes on a simulated 4-CPU machine driving 100-concurrent-tuery bursts: bounds of 2, 4, 8<br/>
   /// and 16 were indistinguishable, because a healthy exchange lasts well under a millisecond. 8 is chosen on qualitative<br/>
   /// grounds: enough lanes that one slow handler cannot stall everything behind it, and never more than the<br/>
   /// <see cref="NamedPipeConnectionListener"/>'s floor of ready listening instances, so even a full cold burst of connects<br/>
   /// finds a ready instance instantly.</remarks>
   const int MaximumConcurrentExchanges = 8;

   ///<summary>How long a sender waits for a free connection before failing loud. Expiring means every connection stayed busy<br/>
   /// for the whole wait: the peer's handlers are not keeping up with this endpoint's send rate.</summary>
   static readonly TimeSpan WaitForAFreeConnectionTimeout = TimeSpan.FromSeconds(10);

   static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);

   readonly EndpointAddress _address;
   readonly IConnectionOpener _connectionOpener;

   //rationale: Compze.Threading's monitors wait by blocking the calling thread. This wait must hold no thread - senders
   //parked on a saturated pool while occupying pool threads is the exact starvation this pool exists to eliminate - and
   //SemaphoreSlim.WaitAsync is the BCL's asynchronous counting wait. Same precedent as CompzeSqliteConnection's write gate.
#pragma warning disable CA2213 //Deliberately never disposed - see the rationale at the bottom of Dispose.
   readonly SemaphoreSlim _exchangeSlots = new(MaximumConcurrentExchanges, MaximumConcurrentExchanges);
#pragma warning restore CA2213
   readonly IMonitor _monitor = IMonitor.New();
   readonly Stack<Stream> _idleConnections = new();
   bool _disposed;

   internal FramedConnectionPool(EndpointAddress address, IConnectionOpener connectionOpener)
   {
      _address = address;
      _connectionOpener = connectionOpener;
   }

   ///<summary>Performs one exchange with the peer: leases a pooled connection — awaiting a free one if all<br/>
   /// <see cref="MaximumConcurrentExchanges"/> are busy, opening one if the pool has none idle — writes the request frame,<br/>
   /// reads the response frame, and returns the connection for the next sender.</summary>
   internal async Task<ExchangeResponse> ExchangeAsync(TransportRequest request, CancellationToken cancellationToken)
   {
      if(!await _exchangeSlots.WaitAsync(WaitForAFreeConnectionTimeout, cancellationToken).caf())
      {
         throw new TessageDispatchingFailedException($"""
                                                      All {MaximumConcurrentExchanges} pooled connections to the {_connectionOpener.TransportName} transport server stayed busy for {WaitForAFreeConnectionTimeout}.
                                                      Address:    {_address.Uri}
                                                      Kind:       {request.Kind}
                                                      Type:       {request.PayloadTypeIdString}
                                                      An exchange holds its connection for the whole round trip, including the serving endpoint's handler execution, so sustained saturation means the peer's handlers are not keeping up with this endpoint's send rate.
                                                      {ThreadPoolStateDescription()}
                                                      """);
      }

      try
      {
#pragma warning disable CA2000 //Ownership is tracked on every path: a clean exchange returns the connection to the pool, a failed one disposes it in the catch below.
         var connection = TakeIdleConnection() ?? await OpenConnectionAsync(request, cancellationToken).caf();
#pragma warning restore CA2000
         try
         {
            await ExchangeFraming.WriteRequestAsync(connection, request, cancellationToken).caf();
            var response = await ExchangeFraming.ReadResponseAsync(connection, cancellationToken).caf();
            ReturnToPool(connection);
            return response;
         }
         catch //Resource cleanup, not handling: the failure mid-exchange leaves the connection's lockstep state unknown, so it is discarded rather than returned - see the class remarks. The pool replaces it on demand.
         {
            await connection.DisposeAsync().caf();
            throw;
         }
      }
      finally
      {
         _exchangeSlots.Release();
      }
   }

   Stream? TakeIdleConnection() => _monitor.Locked(() => _idleConnections.TryPop(out var connection) ? connection : null);

   ///<summary>Opens a fresh connection — the pool's on-demand growth, reached only while holding an exchange slot, which is<br/>
   /// what bounds how many connects a cold burst can attempt at once.</summary>
   async Task<Stream> OpenConnectionAsync(TransportRequest request, CancellationToken cancellationToken)
   {
      try
      {
         return await _connectionOpener.OpenConnectionAsync(_address, ConnectTimeout, cancellationToken).caf();
      }
      catch(TimeoutException exception)
      {
         throw new TessageDispatchingFailedException($"""
                                                      Timed out connecting to the {_connectionOpener.TransportName} transport server.
                                                      Address:    {_address.Uri}
                                                      Kind:       {request.Kind}
                                                      Type:       {request.PayloadTypeIdString}
                                                      {ThreadPoolStateDescription()}
                                                      {exception}
                                                      """);
      }
   }

   ///<summary>Returns a connection whose exchange completed cleanly. After <see cref="Dispose"/> the pool declines returns:<br/>
   /// the connection is closed instead of parked where nothing would ever close it.</summary>
   void ReturnToPool(Stream connection) => _monitor.Locked(() =>
   {
      if(_disposed)
         connection.Dispose();
      else
         _idleConnections.Push(connection);
   });

   ///<summary>Captured into the loud failures above at the moment they are thrown: a wait that expires against a running<br/>
   /// server is the signature of either genuine saturation or of the ThreadPool not servicing continuations — zero free<br/>
   /// workers with pending work says thread-pool starvation, a healthy pool says the peer itself is the bottleneck.</summary>
   static string ThreadPoolStateDescription() => $"ThreadPool at timeout: {ThreadPoolCE.DescribeState()}";

   public void Dispose() => _monitor.Locked(() =>
   {
      if(_disposed) return;
      _disposed = true;
      while(_idleConnections.TryPop(out var connection))
         connection.Dispose();
      //The SemaphoreSlim is deliberately not disposed: it owns an operating-system handle only if AvailableWaitHandle is
      //ever read, which nothing here does, and skipping disposal keeps an in-flight exchange's slot release from throwing
      //ObjectDisposedException during teardown.
   });
}
