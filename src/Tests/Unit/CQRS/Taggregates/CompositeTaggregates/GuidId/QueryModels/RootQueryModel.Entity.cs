using System;
using Compze.Tessaging.Teventive.TeventStore.QueryModels.SelfGeneratingQueryModels;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using JetBrains.Annotations;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.QueryModels;

#pragma warning disable CA1812 // Used via reflection in tuery model infrastructure
[UsedImplicitly] partial class Entity : RootQueryModel.Entity<Entity, Guid, ICompositeTaggregateTevent.Entity, ICompositeTaggregateTevent.Entity.Created, ICompositeTaggregateTevent.Entity.Removed, CompositeTaggregateTevent.Entity.IdGetterSetter>
#pragma warning restore CA1812
{
   public string Name { get; private set; } = string.Empty;
   public Entity(RootQueryModel root) : base(root)
   {
      _entities = RemovableNestedEntity.CreateSelfManagingCollection(this);
      RegisterTeventAppliers()
        .For<ICompositeTaggregateTevent.Entity.PropertyUpdated.Name>(e => Name = e.Name);
   }

   public IReadonlyQueryModelEntityCollection<RemovableNestedEntity, Guid> Entities => _entities.Entities;
   readonly RemovableNestedEntity.CollectionManager _entities;
}