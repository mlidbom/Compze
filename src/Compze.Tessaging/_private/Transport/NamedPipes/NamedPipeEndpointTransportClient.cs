using Compze.Contracts;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.Typermedia;
using Compze.Threading;
using Compze.Tessaging._internal.Transport;

namespace Compze.Tessaging._private.Transport.NamedPipes;

///<summary>The named-pipe implementation of <see cref="IEndpointTransportClient"/>: sends each request over a pooled<br/>
/// persistent connection to the peer's <see cref="NamedPipeEndpointTransportServer"/> — one<br/>
/// <see cref="NamedPipeClientConnectionPool"/> per peer address, created on the first send to that address and living until<br/>
/// this client is disposed with its container.</summary>
///<remarks>A handler exception on the serving side comes back as a well-formed error response — a complete exchange, so the<br/>
/// connection returns to its pool — and is rethrown here as a <see cref="TessageDispatchingFailedException"/> carrying the<br/>
/// serving side's exception type and detail. Only wire-level failures discard connections; that split lives in<br/>
/// <see cref="NamedPipeClientConnectionPool"/>.</remarks>
class NamedPipeEndpointTransportClient : IEndpointTransportClient, IDisposable
{
   readonly IMonitor _monitor = IMonitor.New();
   readonly Dictionary<EndpointAddress, NamedPipeClientConnectionPool> _connectionPoolsByPeerAddress = new();
   bool _disposed;

   public async Task<string> SendAsync(TransportRequest request, EndpointAddress address, CancellationToken cancellationToken = default)
   {
      var response = await PoolFor(address).ExchangeAsync(request, cancellationToken).caf();

      if(!response.Succeeded)
      {
         throw new TessageDispatchingFailedException($"""
                                                      Address:    {address.Uri}
                                                      Kind:       {request.Kind}
                                                      Type:       {request.PayloadTypeIdString}
                                                      Body:
                                                      {request.Body}

                                                      Exception Type: {response.ExceptionType}
                                                      Exception Tessage: {response.ExceptionDetail}
                                                      """);
      }

      return response.Payload;
   }

   NamedPipeClientConnectionPool PoolFor(EndpointAddress address) => _monitor.Locked(() =>
   {
      Contract.State.Assert(!_disposed, () => "A send reached the named-pipe transport client after it was disposed. The endpoint stops sending before its container disposes this client, so a send arriving here is a lifecycle bug in the caller.");
      if(!_connectionPoolsByPeerAddress.TryGetValue(address, out var pool))
         _connectionPoolsByPeerAddress.Add(address, pool = new NamedPipeClientConnectionPool(address));
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
