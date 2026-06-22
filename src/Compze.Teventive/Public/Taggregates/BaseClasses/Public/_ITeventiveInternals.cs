using Compze.Abstractions.Public;
using Compze.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;

public interface ITeventiveInternals<in TTevent, in TTeventImplementation>
    where TTeventImplementation : TaggregateTevent, TTevent
    where TTevent : class, ITaggregateTevent
{
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void PublishInternal(TTeventImplementation theTevent);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void ApplyTeventInternal(TTevent tevent);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] ITeventHandlerRegistrar<TTevent> RegisterTeventAppliersInternal();
}
