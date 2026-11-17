using Compze.Abstractions;
using Compze.Tessaging.TessageTypes;

namespace Compze.Teventive.Taggregates.BaseClasses.Shared;

///<summary>The infrastructure-facing side of a <see cref="SharedTomponent{TTomponentTevent}"/>: what the routing of its owner calls, through its<br/>
/// <see cref="SharedTomponentSlotOfAClass{TOwner,TOwnerTevent,TOwnerTeventImplementation,TTomponentTevent,TAdoptingWrapperTevent}"/>, to route adopted<br/>
/// tevents back into the tomponent, and what a <see cref="SharedTentity{TTentity,TTentityId,TTentityTevent,TTentityCreatedTevent}"/> publishes through.<br/>
/// The shared-teventive counterpart of <see cref="ITeventiveInternals{TTevent,TTeventImplementation}"/>.</summary>
public interface ISharedTomponentInternals<TTomponentTevent>
   where TTomponentTevent : class, ITevent
{
   ///<summary>Applies a tevent still in its slot's adopting wrapper tevent, which the routing of its owner delivers.</summary>
   [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void ApplyAdoptedTeventInternal(IPublisherTevent<TTomponentTevent> adopted);
   [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void PublishInternal(TTomponentTevent tevent);
}
