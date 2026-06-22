using Compze.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;
using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Teventive.TeventStore;

static class TaggregateTeventDataConverter
{
   public static TaggregateTeventData ToTaggregateTeventData(this ITaggregateTevent @this) => new(@this.Id, @this.TaggregateVersion, @this.TaggregateId, @this.UtcTimeStamp);
}
