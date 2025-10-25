using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Sql.Common.TeventStore.Abstractions;

namespace Compze.Tessaging.Teventive.TeventStore;

static class TaggregateTeventDataConverter
{
   internal static TaggregateTeventData ToTaggregateTeventData(this ITaggregateTevent @this) =>
      new TaggregateTeventData(@this.TessageId, @this.TaggregateVersion, @this.TaggregateId, @this.UtcTimeStamp);
}
