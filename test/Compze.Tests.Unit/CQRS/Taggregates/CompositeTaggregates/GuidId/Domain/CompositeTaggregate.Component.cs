using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;

// ReSharper disable RedundantNameQualifier

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain;

partial class Component : CompositeTaggregate.Component<Component, ICompositeTaggregateTevent.Component, CompositeTaggregateTevent.Component>
{
    static readonly TeventRouting<Component, ICompositeTaggregateTevent.Component> Appliers = DeclareAppliers(it => it
       .Entities(static component => component._entities)
       .Entities(static component => component._nonRemovableEntities)
       .Component(static component => component.CComponent)
       .For<ICompositeTaggregateTevent.Component.PropertyUpdated.Name>(static (component, tevent) => component.Name = tevent.Name));

    protected override TeventRouting TeventAppliers => Appliers;

    public Component(CompositeTaggregate parent) : base(parent)
    {
        _entities = Component.Entity.CreateSelfManagingCollection(this);
        _nonRemovableEntities = Component.NonRemovableEntity.CreateSelfManagingCollection(this);
        CComponent = new NestedComponent(this);
    }

    readonly Component.Entity.ICollectionManager _entities;
    readonly Component.NonRemovableEntity.ICollectionManager _nonRemovableEntities;

    public NestedComponent CComponent { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public IReadOnlyEntityCollection<Entity, Guid> Entities => _entities.Entities;
    public IReadOnlyEntityCollection<NonRemovableEntity, Guid> NonRemovableEntities => _nonRemovableEntities.Entities;
    public void Rename(string name) => Publish(new CompositeTaggregateTevent.Component.Renamed(name));

    internal ComponentSnapshot TakeSnapshot() => new(Name, [..Entities.Select(it => it.TakeSnapshot())], [..NonRemovableEntities.Select(it => it.TakeSnapshot())], CComponent.Name);

    internal void RestoreFrom(ComponentSnapshot snapshot)
    {
        Name = snapshot.Name;
        foreach(var entity in snapshot.Entities) _entities.AddWhileRestoring(entity.Id).RestoreFrom(entity);
        foreach(var entity in snapshot.NonRemovableEntities) _nonRemovableEntities.AddWhileRestoring(entity.Id).RestoreFrom(entity);
        CComponent.RestoreFrom(snapshot.NestedComponentName);
    }
    public Component.Entity AddEntity(string name, Guid id) => _entities.AddByPublishing(new CompositeTaggregateTevent.Component.Entity.Created(id, name));
    public Component.NonRemovableEntity AddNonRemovableEntity(string name, Guid id) => _nonRemovableEntities.AddByPublishing(new CompositeTaggregateTevent.Component.NonRemovableEntity.Created(id, name));
}
