using Compze.Abstractions;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using Compze.Teventive;
using Compze.Teventive.Taggregates;
using Compze.Teventive.Taggregates.BaseClasses;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;

class CompositeTaggregate : Taggregate<CompositeTaggregate, ICompositeTaggregateTevent, CompositeTaggregateTevent, ICompositeTaggregateTevent<ICompositeTaggregateTevent>, CompositeTaggregateTevent<CompositeTaggregateTevent>>, ITeventRouted,
                            ISnapshotable<CompositeTaggregate, CompositeTaggregateSnapshot>
{
   static readonly TeventRouting<CompositeTaggregate, ICompositeTaggregateTevent> Appliers = DeclareAppliers(it => it
     .Component(static taggregate => taggregate.Component)
     .Entities(static taggregate => taggregate._entities)
     .For<ICompositeTaggregateTevent.PropertyUpdated.Name>(static (taggregate, tevent) => taggregate.Name = tevent.Name));

   TeventRouting ITeventRouted.TeventAppliers => Appliers;

   public string Name { get; private set; } = string.Empty;
   readonly RemovableEntity.ICollectionManager _entities;
   public Component Component { get; private set; }

   ///<summary>Whether <see cref="RestoreFrom"/> leaves out the entities at the root, as a mistaken restore would: for the specifications of the<br/>
   /// check that a snapshot loses nothing.</summary>
   readonly bool _restoreLeavesOutTheEntities;

   CompositeTaggregate(bool restoreLeavesOutTheEntities = false)
   {
      _restoreLeavesOutTheEntities = restoreLeavesOutTheEntities;
      Component = new Component(this);
      _entities = RemovableEntity.CreateSelfManagingCollection(this);
   }

   public CompositeTaggregate(string name, TaggregateId id) : this() => Publish(new CompositeTaggregateTevent.Created(id, name));

   ///<summary>A new, empty instance, to restore from a snapshot.</summary>
   internal static CompositeTaggregate Empty() => new();

   internal static CompositeTaggregate CreateWhoseRestoreLeavesOutItsEntities(string name, TaggregateId id)
   {
      var taggregate = new CompositeTaggregate(restoreLeavesOutTheEntities: true);
      taggregate.Publish(new CompositeTaggregateTevent.Created(id, name));
      return taggregate;
   }

   internal static CompositeTaggregate EmptyWhoseRestoreLeavesOutItsEntities() => new(restoreLeavesOutTheEntities: true);

   public CompositeTaggregateSnapshot TakeSnapshot() => new(Id, Name, Component.TakeSnapshot(), [..Entities.Select(it => it.TakeSnapshot())]);

   public void RestoreFrom(CompositeTaggregateSnapshot snapshot)
   {
      RestoreIdentity(snapshot.Id);
      Name = snapshot.Name;
      Component.RestoreFrom(snapshot.Component);
      if(_restoreLeavesOutTheEntities) return;
      foreach(var entity in snapshot.Entities) _entities.AddRestored(entity.Id).RestoreFrom(entity);
   }

   ///<summary>Adds an entity as a restore does, though this taggregate isn't being restored: for the specification that it is refused.</summary>
   internal RemovableEntity AddARestoredEntityOutsideARestore() => _entities.AddRestored(Guid.NewGuid());

   public IReadOnlyEntityCollection<RemovableEntity, Guid> Entities => _entities.Entities;
   public RemovableEntity AddEntity(string name) => _entities.AddByPublishing(new CompositeTaggregateTevent.Entity.Created(Guid.NewGuid(), name));
}
