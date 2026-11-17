using System.Collections;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive.Taggregates.BaseClasses._private;

namespace Compze.Teventive.Taggregates.BaseClasses.Shared;

///<summary>An owner's collection of <see cref="SharedTentity{TTentity,TTentityId,TTentityTevent,TTentityCreatedTevent}"/> instances. The collection is itself<br/>
/// a <see cref="SharedTomponent{TTomponentTevent}"/>: it occupies one slot of its owner's class, so every tentity in it publishes through the same adopting<br/>
/// wrapper tevent type. It routes what its slot delivers itself: a <typeparamref name="TTentityCreatedTevent"/> creates and adds a new tentity, and every<br/>
/// tevent reaches the tentity whose <see cref="ISharedTentityTevent{TTentityId}.EntityId"/> it carries - during live publishing and history replay alike.</summary>
public sealed class SharedTentityCollection<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent> : SharedTomponent<TTentityTevent>, IEnumerable<TTentity>
   where TTentity : SharedTentity<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent>
   where TTentityId : struct
   where TTentityTevent : class, ISharedTentityTevent<TTentityId>
   where TTentityCreatedTevent : class, TTentityTevent
{
   readonly EntityCollection<TTentity, TTentityId> _entities = new();

   ///<param name="slot">What <c>Slots.TheSlot.In(owner)</c> returns, for the slot of the owner's class the collection is in.</param>
   public SharedTentityCollection(SharedTomponentSlotInAnOwner<TTentityTevent> slot) : base(slot.Owner, slot.Slot) {}

   public IReadOnlyEntityCollection<TTentity, TTentityId> Entities => _entities;

   ///<summary>Creates a tentity by publishing its <typeparamref name="TTentityCreatedTevent"/> and returns the created instance once the tevent<br/>
   /// has routed back through the owner and the collection.</summary>
   public TTentity AddByPublishing(TTentityCreatedTevent creationTevent)
   {
      Publish(creationTevent);
      return _entities.InCreationOrder[^1];
   }

   private protected override void ApplyAdopted(IPublisherTevent<TTentityTevent> adopted)
   {
      var tevent = adopted.Tevent;
      if(tevent is TTentityCreatedTevent) _entities.Add(CreateTentity(), tevent.EntityId);
      _entities[tevent.EntityId].ApplyAdopted(adopted);
   }

   TTentity CreateTentity() => Constructor.For<TTentity>.WithArguments<SharedTentityCollection<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent>>.Instance(this);

   public IEnumerator<TTentity> GetEnumerator() => Entities.GetEnumerator();
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
