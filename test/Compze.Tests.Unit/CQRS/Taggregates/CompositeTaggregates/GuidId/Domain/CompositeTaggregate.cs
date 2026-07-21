using Compze.Abstractions;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using Compze.Teventive.Taggregates.BaseClasses;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;

class CompositeTaggregate : Taggregate<CompositeTaggregate, ICompositeTaggregateTevent, CompositeTaggregateTevent, ICompositeTaggregateTevent<ICompositeTaggregateTevent>, CompositeTaggregateTevent<CompositeTaggregateTevent>>
{
   public string Name { get; private set; } = string.Empty;
   readonly RemovableEntity.ICollectionManager _entities;
   public Component Component { get; private set; }

   public CompositeTaggregate(string name, TaggregateId id)
   {
      Component = new Component(this);
      _entities = RemovableEntity.CreateSelfManagingCollection(this);

      RegisterTeventAppliers()
        .For<ICompositeTaggregateTevent.PropertyUpdated.Name>(e => Name = e.Name);

      Publish(new CompositeTaggregateTevent.Created(id, name));
   }

   public IReadOnlyEntityCollection<RemovableEntity, Guid> Entities => _entities.Entities;
   public RemovableEntity AddEntity(string name) => _entities.AddByPublishing(new CompositeTaggregateTevent.Entity.Created(Guid.NewGuid(), name));
}
