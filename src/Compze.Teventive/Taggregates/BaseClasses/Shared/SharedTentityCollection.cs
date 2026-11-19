using System.Collections;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Tessaging.TessageTypes;
using Compze.Contracts;
using Compze.Teventive.Taggregates.BaseClasses._internal;
using Compze.Teventive.Taggregates.BaseClasses._private;

namespace Compze.Teventive.Taggregates.BaseClasses.Shared;

///<summary>An owner's collection of <see cref="SharedTentity{TTentity,TTentityId,TTentityTevent,TTentityCreatedTevent}"/> instances. The collection is itself<br/>
/// a <see cref="SharedTomponent{TTomponentTevent}"/>: it occupies one slot of its owner's class, so every tentity in it publishes through the same adopting<br/>
/// wrapper tevent type. It routes what its slot delivers itself: a <typeparamref name="TTentityCreatedTevent"/> creates and adds a new tentity, and every<br/>
/// tevent reaches the tentity whose <see cref="ISharedTentityTevent{TTentityId}.EntityId"/> it carries - during live publishing and history replay alike.</summary>
public sealed class SharedTentityCollection<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent> : SharedTomponent<TTentityTevent>, IEnumerable<TTentity>, IEntityCollectionState
   where TTentity : SharedTentity<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent>
   where TTentityId : struct
   where TTentityTevent : class, ISharedTentityTevent<TTentityId>
   where TTentityCreatedTevent : class, TTentityTevent
{
   readonly EntityCollection<TTentity, TTentityId> _entities = new();

   ///<param name="slot">What <c>Slots.TheSlot.In(owner)</c> returns, for the slot of the owner's class the collection is in.</param>
   public SharedTentityCollection(SharedTomponentSlotInAnOwner<TTentityTevent> slot) : base(slot.Owner, slot.Slot) {}

   ///<summary>None: the collection routes the tevents its slot delivers to the tentity whose id each carries, in code of its own.</summary>
   protected override TeventRouting? TeventAppliers => null;

   public IReadOnlyEntityCollection<TTentity, TTentityId> Entities => _entities;

   ///<summary>Creates a tentity by publishing its <typeparamref name="TTentityCreatedTevent"/> and returns the created instance once the tevent<br/>
   /// has routed back through the owner and the collection.</summary>
   public TTentity AddByPublishing(TTentityCreatedTevent creationTevent)
   {
      Publish(creationTevent);
      return _entities.InCreationOrder[^1];
   }

   ///<summary>Adds a new tentity with the id <paramref name="id"/>, publishing nothing, for the restore of its owner's taggregate from a snapshot to give<br/>
   /// the rest of its state. Only while the taggregate is being restored: from its <c>RestoreIdentity</c> until its restoring code returns to the<br/>
   /// taggregate store restoring it, or else until its first publish. At any other time a tentity is added by publishing its created tevent, with<br/>
   /// <see cref="AddByPublishing"/>.</summary>
   public TTentity AddWhileRestoring(TTentityId id)
   {
      State.Assert(ItsTaggregateIsBeingRestored,
                   () => $"A {typeof(TTentity).Name} is added without its created tevent only while its owner's taggregate is being restored from a snapshot, by its RestoreFrom. Add it by publishing its created tevent, with {nameof(AddByPublishing)}.");
      var tentity = CreateTentity();
      tentity.RestoreId(id);
      _entities.Add(tentity, id);
      return tentity;
   }

   IEnumerable IEntityCollectionState.EntitiesWithTheirIdsInCreationOrder => _entities.InCreationOrderWithTheirIds;

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
