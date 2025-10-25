using System;
using Compze.Tessaging.Teventive;
using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain.Tevents;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain;

#pragma warning disable CA1812 // Used via reflection in aggregate infrastructure
partial class RemovableEntity :
#pragma warning restore CA1812
    CompositeAggregate.RemovableEntity<
        RemovableEntity,
        Guid,
        CompositeAggregateTevent.Entity.Implementation.Root,
        CompositeAggregateTevent.Entity.IRoot,
        CompositeAggregateTevent.Entity.Created,
        CompositeAggregateTevent.Entity.Removed,
        CompositeAggregateTevent.Entity.Implementation.Root.IdGetterSetter>
{
    public string Name { get; private set; } = string.Empty;

    public RemovableEntity(CompositeAggregate compositeAggregate) : base(compositeAggregate)
    {
        _entities = RemovableNestedEntity.CreateSelfManagingCollection(this);
        RegisterTeventAppliers()
           .For<CompositeAggregateTevent.Entity.PropertyUpdated.Name>(e => Name = e.Name);
    }

    public IReadOnlyEntityCollection<RemovableNestedEntity, Guid> Entities => _entities.Entities;
    readonly RemovableNestedEntity.CollectionManager _entities;

    public void Rename(string name) => Publish(new CompositeAggregateTevent.Entity.Implementation.Renamed(name));
    public void Remove() => Publish(new CompositeAggregateTevent.Entity.Implementation.Removed());

    public RemovableNestedEntity AddEntity(string name, Guid id)
        => _entities.AddByPublishing(new CompositeAggregateTevent.Entity.NestedEntity.Implementation.Created(id: id, name: name));
}
