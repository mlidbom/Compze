using Compze.Core.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;

#pragma warning disable CA1812 // Used via reflection in taggregate infrastructure
partial class RemovableEntity :
#pragma warning restore CA1812
    CompositeTaggregate.RemovableEntity<
        RemovableEntity,
        Guid,
        ICompositeTaggregateTevent.Entity,
        CompositeTaggregateTevent.Entity,
        ICompositeTaggregateTevent.Entity.Created,
        ICompositeTaggregateTevent.Entity.Removed,
        CompositeTaggregateTevent.Entity.IdGetterSetter>
{
    public string Name { get; private set; } = string.Empty;

    public RemovableEntity(CompositeTaggregate compositeTaggregate) : base(compositeTaggregate)
    {
        _entities = RemovableNestedEntity.CreateSelfManagingCollection(this);
        RegisterTeventAppliers()
           .For<ICompositeTaggregateTevent.Entity.PropertyUpdated.Name>(e => Name = e.Name);
    }

    public IReadOnlyEntityCollection<RemovableNestedEntity, Guid> Entities => _entities.Entities;
    readonly RemovableNestedEntity.ICollectionManager _entities;

    public void Rename(string name) => Publish(new CompositeTaggregateTevent.Entity.Renamed(name));
    public void Remove() => Publish(new CompositeTaggregateTevent.Entity.Removed());

    public RemovableNestedEntity AddEntity(string name, Guid id)
        => _entities.AddByPublishing(new CompositeTaggregateTevent.Entity.NestedEntity.Created(id: id, name: name));
}
