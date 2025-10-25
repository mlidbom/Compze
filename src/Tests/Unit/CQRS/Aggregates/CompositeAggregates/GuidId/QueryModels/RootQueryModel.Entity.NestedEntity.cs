using System;
using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain.Tevents;
using JetBrains.Annotations;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.QueryModels;

partial class Entity
{
#pragma warning disable CA1812 // Used via reflection in tuery model infrastructure
   [UsedImplicitly]public class RemovableNestedEntity : RemovableNestedEntity<RemovableNestedEntity, Guid, CompositeAggregateTevent.Entity.NestedEntity.IRoot, CompositeAggregateTevent.Entity.NestedEntity.Created, CompositeAggregateTevent.Entity.NestedEntity.Removed, CompositeAggregateTevent.Entity.NestedEntity.Implementation.Root.IdGetterSetter>
#pragma warning restore CA1812
   {
      public string Name { get; private set; } = string.Empty;
      public RemovableNestedEntity(Entity entity) : base(entity)
      {
         RegisterTeventAppliers()
           .For<CompositeAggregateTevent.Entity.NestedEntity.PropertyUpdated.Name>(e => Name = e.Name);
      }
   }
}