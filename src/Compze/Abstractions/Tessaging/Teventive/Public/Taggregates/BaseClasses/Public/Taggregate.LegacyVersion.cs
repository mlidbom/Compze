using System;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Time.Public;

namespace Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;

//Urgent:[Obsolete("Only here to let things compile while inheritors migrate to the version with 5 type parameters")]. Really? If you don't intend to inherit from the Taggregate, what good is it to set the last two type parameters to anything else?
public class Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation> : Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, ITaggregateIdentifyingTevent<TTaggregateTevent>, TaggregateIdentifyingTevent<TTaggregateTevent>>
    where TTaggregate : Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation>
    where TTaggregateTevent : class, ITaggregateTevent
    where TTaggregateTeventImplementation : TaggregateTevent, TTaggregateTevent
{
    [Obsolete("Only for infrastructure", true)]
    protected Taggregate() : this(DateTimeNowTimeSource.Instance) {}

    protected Taggregate(IUtcTimeTimeSource timeSource) : base(timeSource) {}
}
