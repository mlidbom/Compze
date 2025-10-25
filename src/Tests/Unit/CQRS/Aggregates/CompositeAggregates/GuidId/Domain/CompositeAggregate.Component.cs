using System;
using Compze.Tessaging.Teventive;
using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain.Tevents;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain;

partial class Component : CompositeAggregate.Component<Component, CompositeAggregateTevent.Component.Implementation.Root, CompositeAggregateTevent.Component.IRoot>
{
    public Component(CompositeAggregate parent) : base(parent)
    {
        _entities = Component.Entity.CreateSelfManagingCollection(this);
        CComponent = new NestedComponent(this);
        RegisterTeventAppliers()
           .For<CompositeAggregateTevent.Component.PropertyUpdated.Name>(e => Name = e.Name);
    }

    readonly Component.Entity.CollectionManager _entities;

    public NestedComponent CComponent { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public IReadOnlyEntityCollection<Entity, Guid> Entities => _entities.Entities;
    public void Rename(string name) => Publish(new CompositeAggregateTevent.Component.Implementation.Renamed(name));
    public Component.Entity AddEntity(string name, Guid id) => _entities.AddByPublishing(new CompositeAggregateTevent.Component.Entity.Implementation.Created(id, name));
}
