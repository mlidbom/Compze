using System;
using Compze.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels;
using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain.Events;
using JetBrains.Annotations;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.QueryModels;

[UsedImplicitly] partial class Entity : RootQueryModel.Entity<Entity, Guid, CompositeAggregateEvent.Entity.IRoot, CompositeAggregateEvent.Entity.Created, CompositeAggregateEvent.Entity.Removed, CompositeAggregateEvent.Entity.Implementation.Root.IdGetterSetter>
{
   public string Name { get; private set; } = string.Empty;
   public Entity(RootQueryModel root) : base(root)
   {
      _entities = RemovableNestedEntity.CreateSelfManagingCollection(this);
      RegisterEventAppliers()
        .For<CompositeAggregateEvent.Entity.PropertyUpdated.Name>(e => Name = e.Name);
   }

   public IReadonlyQueryModelEntityCollection<RemovableNestedEntity, Guid> Entities => _entities.Entities;
   readonly RemovableNestedEntity.CollectionManager _entities;
}