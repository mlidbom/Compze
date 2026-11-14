using Compze.Hosting.AcrossMachines;
using Compze.Tessaging.Endpoints.Discovery;

namespace Compze.Tests.Integration.AcrossMachines;

///<summary>What the specifications of <see cref="NetworkEndpointRegistry"/> need of it that it has no reason to offer anyone else.</summary>
static class NetworkEndpointRegistryCE
{
   static readonly TimeSpan TimeToComeToListThem = TimeSpan.FromSeconds(20);
   static readonly TimeSpan TimeBetweenLooks = TimeSpan.FromMilliseconds(50);

   extension(NetworkEndpointRegistry @this)
   {
      ///<summary>What the registry lists once it lists exactly <paramref name="addresses"/>, ordered by address. A registry<br/>
      /// lists what its server told it, which arrives a moment after it changed, so the registry is given time to come to<br/>
      /// list them. If it never does, this returns what it lists instead, for the assertion to show.</summary>
      internal IReadOnlyList<EndpointAddress> AddressesListedOnceTheyAre(params EndpointAddress[] addresses)
      {
         var deadline = DateTime.UtcNow + TimeToComeToListThem;
         while(true)
         {
            var listed = @this.ServerEndpointAddresses.OrderBy(it => it.Uri.AbsoluteUri, StringComparer.Ordinal).ToList();
            if(listed.ToHashSet().SetEquals(addresses) || DateTime.UtcNow >= deadline) return listed;
            @this.AwaitPossibleMembershipChange(TimeBetweenLooks, CancellationToken.None);
         }
      }
   }
}
