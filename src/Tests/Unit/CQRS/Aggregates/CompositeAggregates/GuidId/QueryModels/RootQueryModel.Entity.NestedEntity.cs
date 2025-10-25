using System;
using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain.Events;
using JetBrains.Annotations;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.QueryModels;

partial class Entity
{
#pragma warning disable CA1812 // Used via reflection in tuery model infrastructure
   [UsedImplicitly]public class RemovableNestedEntity : RemovableNestedEntity<RemovableNestedEntity, Guid, CompositeAggregateEvent.Entity.NestedEntity.IRoot, CompositeAggregateEvent.Entity.NestedEntity.Created, CompositeAggregateEvent.Entity.NestedEntity.Removed, CompositeAggregateEvent.Entity.NestedEntity.Implementation.Root.IdGetterSetter>
#pragma warning restore CA1812
   {
      public string Name { get; private set; } = string.Empty;
      public RemovableNestedEntity(Entity entity) : base(entity)
      {
         RegisterEventAppliers()
           .For<CompositeAggregateEvent.Entity.NestedEntity.PropertyUpdated.Name>(e => Name = e.Name);
      }
   }
}