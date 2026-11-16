using Compze.Abstractions;

namespace Compze.Teventive.TaggregateStores._internal;

///<summary>Puts <see cref="TaggregateId"/>s in id order, in which every taggregate store gives the taggregates of a class: the order of<br/>
/// the ids' <see cref="Guid"/>s, which is the order of their text. The same order whichever store keeps the class.</summary>
static class TaggregateIdCE
{
   extension(IEnumerable<TaggregateId> @this)
   {
      ///<summary>These ids, in id order.</summary>
      internal IEnumerable<TaggregateId> InIdOrder() => @this.OrderBy(it => it.Value);
   }
}
