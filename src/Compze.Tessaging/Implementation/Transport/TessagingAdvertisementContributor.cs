using Compze.TypeIdentifiers;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Internals.Transport;

namespace Compze.Tessaging.Implementation.Transport;

///<summary>The TessageBus side's share of the endpoint's one advertisement: the remotable tevent subscriptions and exactly-once<br/>
/// tommand types the endpoint's <see cref="TessageHandlerRoster"/> serves.</summary>
class TessagingAdvertisementContributor(TessageHandlerRoster roster) : IEndpointAdvertisementContributor
{
   readonly TessageHandlerRoster _roster = roster;
   public ISet<TypeId> AdvertisedRemoteTessageTypeIds() => _roster.HandledRemoteTessageBusTypeIds();
}
