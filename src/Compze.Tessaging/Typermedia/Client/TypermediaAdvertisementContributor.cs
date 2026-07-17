using Compze.TypeIdentifiers;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Internals.Transport;

namespace Compze.Tessaging.Typermedia.Client;

///<summary>The Typermedia side's share of the endpoint's one advertisement: the remotable tuery and typermedia tommand types<br/>
/// the endpoint's <see cref="TessageHandlerRoster"/> serves.</summary>
class TypermediaAdvertisementContributor(TessageHandlerRoster roster) : IEndpointAdvertisementContributor
{
   readonly TessageHandlerRoster _roster = roster;
   public ISet<TypeId> AdvertisedRemoteTessageTypeIds() => _roster.HandledRemoteTypermediaTypeIds();
}
