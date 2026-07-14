using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions;

namespace Compze.Teventive.TeventStore;

static class TaggregateTeventDataConverter
{
   public static TaggregateTeventData ToTaggregateTeventData(this ITaggregateTevent @this) => new(@this.Id, @this.TaggregateVersion, @this.TaggregateId, @this.UtcTimeStamp);
}
