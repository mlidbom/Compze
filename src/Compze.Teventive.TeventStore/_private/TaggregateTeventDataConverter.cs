using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Teventive.TeventStore.Abstractions._internal.SqlLayer.Abstractions;

namespace Compze.Teventive.TeventStore._private;

static class TaggregateTeventDataConverter
{
   public static TaggregateTeventData ToTaggregateTeventData(this ITaggregateTevent @this) => new(@this.Id, @this.TaggregateVersion, @this.TaggregateId, @this.UtcTimeStamp);
}
