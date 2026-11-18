using Compze.Tessaging.TessageTypes;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses.Shared;

#pragma warning disable CA1715 // Nested tevent interfaces follow the semantic events naming convention

namespace Compze.Tests.Unit.CQRS.Taggregates.SnapshotRoundTrips;

///<summary>A set of tags: a shared tomponent, in a slot that belongs to its owner's class.</summary>
class TagSet : SharedTomponent<ITagSetTevent>
{
   static readonly TeventRouting<TagSet, ITagSetTevent> Appliers = DeclareAppliers<TagSet>(it => it
     .For<ITagSetTevent.Added>(static (tags, tevent) => tags._tags.Add(tevent.Tag)));

   protected override TeventRouting TeventAppliers => Appliers;

   readonly HashSet<string> _tags = [];

   public TagSet(SharedTomponentSlotInAnOwner<ITagSetTevent> slot) : base(slot) {}

   public IReadOnlySet<string> All => _tags;

   public void Add(string tag) => Publish(new TagSetTevent.Added(tag));

   internal void RestoreFrom(IEnumerable<string> tags) => _tags.UnionWith(tags);
}

interface ITagSetTevent : ITevent
{
   interface Added : ITagSetTevent { string Tag { get; } }
}

class TagSetTevent : ITagSetTevent
{
   internal class Added(string tag) : TagSetTevent, ITagSetTevent.Added { public string Tag { get; } = tag; }
}
