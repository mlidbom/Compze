using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Threading;

namespace Compze.Tests.Integration.Hosting;

///<summary>Serves the live hosts' endpoint addresses to an endpoint's router — the discovery a production suite gets from a<br/>
/// shared registry — and counts the router's reads, so a specification can await the reconciliation pass that acts on a<br/>
/// membership change (<see cref="AwaitTwoReadsCompletingAfterNow"/>) instead of sleeping and hoping.</summary>
class AddressesOfTheLiveHosts : IEndpointRegistry
{
   readonly IMonitor _monitor = IMonitor.New();
   readonly List<IEndpointHost> _liveHosts = [];
   long _reads;

   internal void Add(IEndpointHost host) => _monitor.Locked(() => _liveHosts.Add(host));
   internal void Remove(IEndpointHost host) => _monitor.Locked(() => _liveHosts.Remove(host));

   public IEnumerable<EndpointAddress> ServerEndpointAddresses => _monitor.Locked(() =>
   {
      _reads++;
      return (IReadOnlyList<EndpointAddress>)
      [
         .._liveHosts.SelectMany(host => host.Endpoints)
                     .OfType<Endpoint>()
                     .Where(endpoint => endpoint.Address is not null)
                     .Select(endpoint => endpoint.Address!)
      ];
   });

   ///<summary>Returns once two <see cref="ServerEndpointAddresses"/> reads have completed after the call: the first read may<br/>
   /// belong to a reconciliation pass that was already mid-flight on the old membership, but the second belongs to a pass that<br/>
   /// started afterwards — so by then a full pass has acted on the current membership: a removed host's connection is dropped,<br/>
   /// an added host's connection is up with its delivery stream draining.</summary>
   internal void AwaitTwoReadsCompletingAfterNow()
   {
      var readsAtCall = _monitor.Locked(() => _reads);
      var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
      while(_monitor.Locked(() => _reads) < readsAtCall + 2)
      {
         if(DateTime.UtcNow > deadline) throw new TimeoutException("The router stopped reading the registry.");
         Thread.Sleep(20);
      }
   }
}
