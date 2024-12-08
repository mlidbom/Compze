using System;
using Compze.Persistence.EventStore.Aggregates;
using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain.Events;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain;

partial class Component : CompositeAggregate.Component<Component, CompositeAggregateEvent.Component.Implementation.Root, CompositeAggregateEvent.Component.IRoot>
{
   public Component(CompositeAggregate compositeAggregate) : base(compositeAggregate)
   {
      _entities = Component.Entity.CreateSelfManagingCollection(this);
      CComponent = new NestedComponent(this);
      RegisterEventAppliers()
        .For<CompositeAggregateEvent.Component.PropertyUpdated.Name>(e => Name = e.Name);
   }

   readonly Component.Entity.CollectionManager _entities;

   public NestedComponent CComponent { get; private set; }

   public string Name { get; private set; } = string.Empty;
   public IReadOnlyEntityCollection<Entity, Guid> Entities => _entities.Entities;
   public void Rename(string name) => Publish(new CompositeAggregateEvent.Component.Implementation.Renamed(name));
   public Component.Entity AddEntity(string name, Guid id) => _entities.AddByPublishing(new CompositeAggregateEvent.Component.Entity.Implementation.Created(id, name));
}