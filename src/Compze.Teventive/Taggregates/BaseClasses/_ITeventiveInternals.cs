using Compze.Abstractions;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive.Taggregates.BaseClasses;

public interface ITeventiveInternals<in TTevent, in TTeventImplementation>
    where TTeventImplementation : TaggregateTevent, TTevent
    where TTevent : class, ITaggregateTevent
{
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void PublishInternal(TTeventImplementation theTevent);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void ApplyTeventInternal(TTevent tevent);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] ITeventSubscriber<TTevent> RegisterTeventAppliersInternal();
}
