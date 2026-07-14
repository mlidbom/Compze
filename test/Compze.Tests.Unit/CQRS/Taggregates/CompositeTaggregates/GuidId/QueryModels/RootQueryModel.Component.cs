using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using Compze.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;

// ReSharper disable RedundantNameQualifier

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.QueryModels;

partial class Component : RootQueryModel.Component<Component, ICompositeTaggregateTevent.Component>
{
   public Component(RootQueryModel root) : base(root)
   {
      _entities = Component.Entity.CreateSelfManagingCollection(this);
      _nonRemovableEntities = Component.NonRemovableEntity.CreateSelfManagingCollection(this);
      CComponent = new NestedComponent(this);
      RegisterTeventAppliers()
        .For<ICompositeTaggregateTevent.Component.PropertyUpdated.Name>(e => Name = e.Name);
   }

   readonly Component.Entity.ICollectionManager _entities;
   readonly Component.NonRemovableEntity.CollectionManager _nonRemovableEntities;
   // ReSharper disable once NotAccessedField.Local
   public NestedComponent CComponent { get; private set; }

   public string Name { get; private set; } = string.Empty;
   public IReadonlyQueryModelEntityCollection<Entity, Guid> Entities => _entities.Entities;
   public IReadonlyQueryModelEntityCollection<NonRemovableEntity, Guid> NonRemovableEntities => _nonRemovableEntities.Entities;
}