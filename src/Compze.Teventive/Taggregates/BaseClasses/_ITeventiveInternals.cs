using Compze.Abstractions;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive.Taggregates.BaseClasses;

///<summary>A taggregate, or a part of one, that publishes the tevents of <typeparamref name="TTevent"/>: what its parts publish through, and what<br/>
/// its routing routes their tevents with.</summary>
public interface ITeventiveInternals<in TTevent, in TTeventImplementation>
    where TTeventImplementation : TaggregateTevent, TTevent
    where TTevent : class, ITaggregateTevent
{
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void PublishInternal(TTeventImplementation theTevent);

    ///<summary>The routing of this teventive's class to its tevent appliers: what a part constructed in it asserts routes to it.</summary>
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] TeventRouting TeventAppliersInternal { get; }
}
