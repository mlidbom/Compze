using System;
using Compze.Persistence.EventStore.Aggregates;
using Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain.Events;

namespace Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain;

partial class RemovableEntity : CompositeAggregate.RemovableEntity<RemovableEntity, Guid, CompositeAggregateEvent.Entity.Implementation.Root, CompositeAggregateEvent.Entity.IRoot, CompositeAggregateEvent.Entity.Created, CompositeAggregateEvent.Entity.Removed, CompositeAggregateEvent.Entity.Implementation.Root.IdGetterSetter>
{
   public string Name { get; private set; } = string.Empty;

   public RemovableEntity(CompositeAggregate compositeAggregate) : base(compositeAggregate)
   {
      _entities = RemovableNestedEntity.CreateSelfManagingCollection(this);
      RegisterEventAppliers()
        .For<CompositeAggregateEvent.Entity.PropertyUpdated.Name>(e => Name = e.Name);
   }

   public IReadOnlyEntityCollection<RemovableNestedEntity, Guid> Entities => _entities.Entities;
   readonly RemovableNestedEntity.CollectionManager _entities;

   public void Rename(string name) => Publish(new CompositeAggregateEvent.Entity.Implementation.Renamed(name));
   public void Remove() => Publish(new CompositeAggregateEvent.Entity.Implementation.Removed());

   public RemovableNestedEntity AddEntity(string name, Guid id)
      => _entities.AddByPublishing(new CompositeAggregateEvent.Entity.NestedEntity.Implementation.Created(id: id, name: name));
}
