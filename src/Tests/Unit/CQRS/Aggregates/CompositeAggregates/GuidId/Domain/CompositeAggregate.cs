using System;
using Compze.Abstractions.Time.Public;
using Compze.Tessaging.Teventive;
using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain.Tevents;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain;

class CompositeAggregate :
    Aggregate<CompositeAggregate,
        CompositeAggregateTevent.ICompositeAggregateTevent,
        CompositeAggregateTevent.Implementation.Root>
{
    public string Name { get; private set; } = string.Empty;
    readonly RemovableEntity.CollectionManager _entities;
    public Component Component { get; private set; }

    public CompositeAggregate(string name, Guid id) : base(new DateTimeNowTimeSource())
    {
        Component = new Component(this);
        _entities = RemovableEntity.CreateSelfManagingCollection(this);

        RegisterTeventAppliers()
           .For<CompositeAggregateTevent.PropertyUpdated.Name>(e => Name = e.Name);

        Publish(new CompositeAggregateTevent.Implementation.Created(id, name));
    }

    public IReadOnlyEntityCollection<RemovableEntity, Guid> Entities => _entities.Entities;
    public RemovableEntity AddEntity(string name) => _entities.AddByPublishing(new CompositeAggregateTevent.Entity.Implementation.Created(Guid.NewGuid(), name));
}
