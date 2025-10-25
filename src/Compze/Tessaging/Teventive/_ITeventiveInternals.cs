using System;
using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Tessaging.Teventive;

public interface ITeventiveInternals<in TTevent, in TTeventImplementation>
    where TTeventImplementation : AggregateTevent, TTevent
    where TTevent : class, IAggregateTevent
{
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void PublishInternal(TTeventImplementation theTevent);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void ApplyTeventInternal(TTevent @tevent);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] ITeventHandlerRegistrar<TTevent> RegisterTeventAppliersInternal();
}
