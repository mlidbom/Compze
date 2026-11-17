using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;

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
        CompositeTaggregateTevent.Entity.IdGetterSetter>, ITeventRouted
{
    static readonly TeventRouting<RemovableEntity, ICompositeTaggregateTevent.Entity> Appliers = DeclareAppliers(it => it
       .Entities(static entity => entity._entities)
       .For<ICompositeTaggregateTevent.Entity.PropertyUpdated.Name>(static (entity, tevent) => entity.Name = tevent.Name));

    TeventRouting ITeventRouted.TeventAppliers => Appliers;

    public string Name { get; private set; } = string.Empty;

    public RemovableEntity(CompositeTaggregate compositeTaggregate) : base(compositeTaggregate) =>
        _entities = RemovableNestedEntity.CreateSelfManagingCollection(this);

    public IReadOnlyEntityCollection<RemovableNestedEntity, Guid> Entities => _entities.Entities;
    readonly RemovableNestedEntity.ICollectionManager _entities;

    public void Rename(string name) => Publish(new CompositeTaggregateTevent.Entity.Renamed(name));
    public void Remove() => Publish(new CompositeTaggregateTevent.Entity.Removed());

    public RemovableNestedEntity AddEntity(string name, Guid id)
        => _entities.AddByPublishing(new CompositeTaggregateTevent.Entity.NestedEntity.Created(id: id, name: name));
}
