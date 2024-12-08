using System;
using Compze.GenericAbstractions.Time;
using Compze.Persistence.EventStore.Aggregates;
using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain.Events;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain;

class CompositeAggregate : Aggregate<CompositeAggregate, CompositeAggregateEvent.Implementation.Root, CompositeAggregateEvent.ICompositeAggregateEvent>
{
   public string Name { get; private set; } = string.Empty;
   readonly RemovableEntity.CollectionManager _entities;
   public Component Component { get; private set; }

   public CompositeAggregate(string name, Guid id) : base(new DateTimeNowTimeSource())
   {
      Component = new Component(this);
      _entities = RemovableEntity.CreateSelfManagingCollection(this);

      RegisterEventAppliers()
        .For<CompositeAggregateEvent.PropertyUpdated.Name>(e => Name = e.Name);

      Publish(new CompositeAggregateEvent.Implementation.Created(id, name));
   }

   public IReadOnlyEntityCollection<RemovableEntity, Guid> Entities => _entities.Entities;
   public RemovableEntity AddEntity(string name) => _entities.AddByPublishing(new CompositeAggregateEvent.Entity.Implementation.Created(Guid.NewGuid(), name));
}