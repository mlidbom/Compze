using System.Collections;
using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Teventive.Taggregates.BaseClasses.Shared;

///<summary>An owner's collection of <see cref="SharedTentity{TTentity,TTentityId,TTentityTevent,TTentityCreatedTevent}"/> instances. The collection is itself<br/>
/// a <see cref="SharedTomponent{TTomponentTevent}"/>: it occupies one <see cref="ISharedTomponentSlot{TTomponentTevent}"/> on the owner, so every tentity in it<br/>
/// publishes through the same adopting wrapper tevent type. Its appliers do the per-instance work: a <typeparamref name="TTentityCreatedTevent"/> creates and<br/>
/// registers a new tentity, and every tevent is routed to the tentity whose <see cref="ISharedTentityTevent{TTentityId}.EntityId"/> it carries -<br/>
/// during live publishing and history replay alike.</summary>
public class SharedTentityCollection<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent> : SharedTomponent<TTentityTevent>, IEnumerable<TTentity>
   where TTentity : SharedTentity<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent>
   where TTentityId : struct
   where TTentityTevent : class, ISharedTentityTevent<TTentityId>
   where TTentityCreatedTevent : class, TTentityTevent
{
   readonly EntityCollection<TTentity, TTentityId> _entities = new();

   public SharedTentityCollection(ISharedTomponentSlot<TTentityTevent> slot) : base(slot) =>
      RegisterTeventAppliers()
        .For<TTentityCreatedTevent>(tevent => _entities.Add(CreateTentity(), tevent.EntityId))
        .For<TTentityTevent>(tevent => _entities[tevent.EntityId].ApplyTevent(tevent));

   public IReadOnlyEntityCollection<TTentity, TTentityId> Entities => _entities;

   ///<summary>Creates a tentity by publishing its <typeparamref name="TTentityCreatedTevent"/> and returns the created instance once the tevent<br/>
   /// has routed back through the owner and the collection's appliers have run.</summary>
   public TTentity AddByPublishing(TTentityCreatedTevent creationTevent)
   {
      Publish(creationTevent);
      return _entities.InCreationOrder[^1];
   }

   TTentity CreateTentity() => Constructor.For<TTentity>.WithArguments<SharedTentityCollection<TTentity, TTentityId, TTentityTevent, TTentityCreatedTevent>>.Instance(this);

   public IEnumerator<TTentity> GetEnumerator() => Entities.GetEnumerator();
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
