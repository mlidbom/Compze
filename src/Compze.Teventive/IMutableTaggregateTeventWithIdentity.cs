using Compze.Abstractions;
using Compze.Tessaging;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive;

public interface IMutableTaggregateTeventWithIdentity : IMutableTaggregateTevent, ITaggregateTeventWithIdentity
{
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void SetTessageIdInternal(TessageId tessageId);
}
