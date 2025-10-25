using System;
using Compze.Abstractions.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;

#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
partial class RemovableEntity :
#pragma warning restore CA1812
    CompositeTaggregate.RemovableEntity<
        RemovableEntity,
        Guid,
        CompositeTaggregateTevent.Entity.Implementation.Root,
        CompositeTaggregateTevent.Entity.IRoot,
        CompositeTaggregateTevent.Entity.Created,
        CompositeTaggregateTevent.Entity.Removed,
        CompositeTaggregateTevent.Entity.Implementation.Root.IdGetterSetter>
{
    public string Name { get; private set; } = string.Empty;

    public RemovableEntity(CompositeTaggregate compositeTaggregate) : base(compositeTaggregate)
    {
        _entities = RemovableNestedEntity.CreateSelfManagingCollection(this);
        RegisterTeventAppliers()
           .For<CompositeTaggregateTevent.Entity.PropertyUpdated.Name>(e => Name = e.Name);
    }

    public IReadOnlyEntityCollection<RemovableNestedEntity, Guid> Entities => _entities.Entities;
    readonly RemovableNestedEntity.CollectionManager _entities;

    public void Rename(string name) => Publish(new CompositeTaggregateTevent.Entity.Implementation.Renamed(name));
    public void Remove() => Publish(new CompositeTaggregateTevent.Entity.Implementation.Removed());

    public RemovableNestedEntity AddEntity(string name, Guid id)
        => _entities.AddByPublishing(new CompositeTaggregateTevent.Entity.NestedEntity.Implementation.Created(id: id, name: name));
}
