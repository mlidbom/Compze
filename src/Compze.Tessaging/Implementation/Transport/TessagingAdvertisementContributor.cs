using Compze.TypeIdentifiers;
using Compze.Tessaging.Internals.Transport;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;

namespace Compze.Tessaging.Implementation.Transport;

///<summary>The TessageBus side's share of the endpoint's one advertisement: the remotable tevent subscriptions and tommand<br/>
/// types the endpoint's <see cref="ITessageHandlerRegistry"/> serves.</summary>
class TessagingAdvertisementContributor(ITessageHandlerRegistry handlerRegistry) : IEndpointAdvertisementContributor
{
   readonly ITessageHandlerRegistry _handlerRegistry = handlerRegistry;
   public ISet<TypeId> AdvertisedRemoteTessageTypeIds() => _handlerRegistry.HandledRemoteTessageTypeIds();
}
