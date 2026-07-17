using Compze.TypeIdentifiers;
using Compze.Tessaging.Internals.Transport;
using Compze.Tessaging.Typermedia.HandlerRegistration;

namespace Compze.Tessaging.Typermedia.Client;

///<summary>The Typermedia side's share of the endpoint's one advertisement: the remotable tuery and typermedia tommand types<br/>
/// the endpoint's <see cref="ITypermediaHandlerRegistry"/> serves.</summary>
class TypermediaAdvertisementContributor(ITypermediaHandlerRegistry handlerRegistry) : IEndpointAdvertisementContributor
{
   readonly ITypermediaHandlerRegistry _handlerRegistry = handlerRegistry;
   public ISet<TypeId> AdvertisedRemoteTessageTypeIds() => _handlerRegistry.HandledRemoteTypermediaTypeIds();
}
