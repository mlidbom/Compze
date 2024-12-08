using System;
using Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain.Events;
using JetBrains.Annotations;

namespace Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain;

[UsedImplicitly] partial class RemovableEntity
{
   public class RemovableNestedEntity : RemovableNestedEntity<RemovableNestedEntity, Guid, CompositeAggregateEvent.Entity.NestedEntity.Implementation.Root, CompositeAggregateEvent.Entity.NestedEntity.IRoot, CompositeAggregateEvent.Entity.NestedEntity.Created, CompositeAggregateEvent.Entity.NestedEntity.Removed, CompositeAggregateEvent.Entity.NestedEntity.Implementation.Root.IdGetterSetter>
   {
      public string Name { get; private set; } = string.Empty;

      public RemovableNestedEntity(RemovableEntity removableEntity) : base(removableEntity)
      {
         RegisterEventAppliers()
           .For<CompositeAggregateEvent.Entity.NestedEntity.PropertyUpdated.Name>(e => Name = e.Name);
      }

      public void Rename(string name) => Publish(new CompositeAggregateEvent.Entity.NestedEntity.Implementation.Renamed(name: name));
      public void Remove() => Publish(new CompositeAggregateEvent.Entity.NestedEntity.Implementation.Removed());
   }
}
