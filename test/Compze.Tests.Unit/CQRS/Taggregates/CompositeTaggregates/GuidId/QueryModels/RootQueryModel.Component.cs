using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using Compze.Teventive;
using Compze.Teventive.Taggregates.BaseClasses;

// ReSharper disable RedundantNameQualifier

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.QueryModels;

partial class Component : RootQueryModel.Component<Component, ICompositeTaggregateTevent.Component>, ITeventRouted
{
   static readonly TeventRouting<Component, ICompositeTaggregateTevent.Component> Appliers = DeclareAppliers(it => it
     .Entities(static component => component._entities)
     .Entities(static component => component._nonRemovableEntities)
     .Component(static component => component.CComponent)
     .For<ICompositeTaggregateTevent.Component.PropertyUpdated.Name>(static (component, tevent) => component.Name = tevent.Name));

   TeventRouting ITeventRouted.TeventAppliers => Appliers;

   public Component(RootQueryModel root) : base(root)
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
}
