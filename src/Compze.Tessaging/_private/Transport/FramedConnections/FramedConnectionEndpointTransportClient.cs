using Compze.Contracts;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.Typermedia;
using Compze.Threading;
using Compze.Tessaging._internal.Transport;

namespace Compze.Tessaging._private.Transport.FramedConnections;

///<summary>The framed-connection transports' implementation of <see cref="IEndpointTransportClient"/>: sends each request<br/>
/// over a pooled persistent connection to the peer's <see cref="FramedConnectionEndpointTransportServer"/> — one<br/>
/// <see cref="FramedConnectionPool"/> per peer address, created on the first send to that address and living until<br/>
/// this client is disposed with its container. Which transport it speaks is decided by the <see cref="IConnectionOpener"/><br/>
/// it is given.</summary>
///<remarks>A handler exception on the serving side comes back as a well-formed error response — a complete exchange, so the<br/>
/// connection returns to its pool — and is rethrown here as a <see cref="TessageDispatchingFailedException"/> carrying the<br/>
/// serving side's exception type and detail (<see cref="ExchangeResponse.PayloadOrThrowTheServingSidesFailure"/>). Only<br/>
/// wire-level failures discard connections; that split lives in <see cref="FramedConnectionPool"/>.</remarks>
class FramedConnectionEndpointTransportClient : IEndpointTransportClient, IDisposable
{
   readonly IConnectionOpener _connectionOpener;
   readonly IMonitor _monitor = IMonitor.New();
   readonly Dictionary<EndpointAddress, FramedConnectionPool> _connectionPoolsByPeerAddress = new();
   bool _disposed;

   internal FramedConnectionEndpointTransportClient(IConnectionOpener connectionOpener) => _connectionOpener = connectionOpener;

   public async Task<string> SendAsync(TransportRequest request, EndpointAddress address, CancellationToken cancellationToken = default)
   {
      var response = await PoolFor(address).ExchangeAsync(request, cancellationToken).caf();
      return response.PayloadOrThrowTheServingSidesFailure(request, address);
   }

   FramedConnectionPool PoolFor(EndpointAddress address) => _monitor.Locked(() =>
   {
      State.Assert(!_disposed, () => $"A send reached the {_connectionOpener.TransportName} transport client after it was disposed. The endpoint stops sending before its container disposes this client, so a send arriving here is a lifecycle bug in the caller.");
      //bug: A pool is kept until this client is disposed, also when its address is gone for good. A peer that restarts
      //   listens at a new address, so every restart of a peer leaves one more pool here, holding its connections to the
      //   old address. Nothing tells this client that an address is gone: the router knows, from the registry.
      if(!_connectionPoolsByPeerAddress.TryGetValue(address, out var pool))
         _connectionPoolsByPeerAddress.Add(address, pool = new FramedConnectionPool(address, _connectionOpener));
      return pool;
   });

   public void Dispose() => _monitor.Locked(() =>
   {
      if(_disposed) return;
      _disposed = true;
      foreach(var pool in _connectionPoolsByPeerAddress.Values)
         pool.Dispose();
      _connectionPoolsByPeerAddress.Clear();
   });
}
