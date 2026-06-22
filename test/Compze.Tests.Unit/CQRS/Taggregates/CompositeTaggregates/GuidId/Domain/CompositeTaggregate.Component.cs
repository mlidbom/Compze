using Compze.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
// ReSharper disable RedundantNameQualifier

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;

partial class Component : CompositeTaggregate.Component<Component, ICompositeTaggregateTevent.Component, CompositeTaggregateTevent.Component>
{
    public Component(CompositeTaggregate parent) : base(parent)
    {
        _entities = Component.Entity.CreateSelfManagingCollection(this);
        _nonRemovableEntities = Component.NonRemovableEntity.CreateSelfManagingCollection(this);
        CComponent = new NestedComponent(this);
        RegisterTeventAppliers()
           .For<ICompositeTaggregateTevent.Component.PropertyUpdated.Name>(e => Name = e.Name);
    }

    readonly Component.Entity.ICollectionManager _entities;
    readonly Component.NonRemovableEntity.ICollectionManager _nonRemovableEntities;

    public NestedComponent CComponent { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public IReadOnlyEntityCollection<Entity, Guid> Entities => _entities.Entities;
    public IReadOnlyEntityCollection<NonRemovableEntity, Guid> NonRemovableEntities => _nonRemovableEntities.Entities;
    public void Rename(string name) => Publish(new CompositeTaggregateTevent.Component.Renamed(name));
    public Component.Entity AddEntity(string name, Guid id) => _entities.AddByPublishing(new CompositeTaggregateTevent.Component.Entity.Created(id, name));
    public Component.NonRemovableEntity AddNonRemovableEntity(string name, Guid id) => _nonRemovableEntities.AddByPublishing(new CompositeTaggregateTevent.Component.NonRemovableEntity.Created(id, name));
}
