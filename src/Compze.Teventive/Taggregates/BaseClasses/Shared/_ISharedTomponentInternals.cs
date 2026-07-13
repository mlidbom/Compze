using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;

namespace Compze.Teventive.Taggregates.BaseClasses.Shared;

///<summary>The infrastructure-facing side of a <see cref="SharedTomponent{TTomponentTevent}"/>: what its <see cref="ISharedTomponentSlot{TTomponentTevent}"/><br/>
/// calls to route adopted tevents back into the tomponent, and what a <see cref="SharedTentity{TTentity,TTentityId,TTentityTevent,TTentityCreatedTevent}"/><br/>
/// publishes through. The shared-teventive counterpart of <see cref="ITeventiveInternals{TTevent,TTeventImplementation}"/>.</summary>
public interface ISharedTomponentInternals<TTomponentTevent>
   where TTomponentTevent : class, ITevent
{
   [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void ApplyTeventInternal(TTomponentTevent tevent);
   [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void PublishInternal(TTomponentTevent tevent);
}
