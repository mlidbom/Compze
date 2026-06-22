using Compze.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;

namespace Compze.Tessaging.Teventive.TeventStore;

static class TaggregateTeventDataConverter
{
   public static TaggregateTeventData ToTaggregateTeventData(this ITaggregateTevent @this) => new(@this.Id, @this.TaggregateVersion, @this.TaggregateId, @this.UtcTimeStamp);
}
