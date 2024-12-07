using System;
using Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain.Events;
using JetBrains.Annotations;

namespace Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.QueryModels;

partial class Entity
{
   [UsedImplicitly]public class RemovableNestedEntity : RemovableNestedEntity<RemovableNestedEntity, Guid, RootEvent.Entity.NestedEntity.IRoot, RootEvent.Entity.NestedEntity.Created, RootEvent.Entity.NestedEntity.Removed, RootEvent.Entity.NestedEntity.Implementation.Root.IdGetterSetter>
   {
      public string Name { get; private set; } = string.Empty;
      public RemovableNestedEntity(Entity entity) : base(entity)
      {
         RegisterEventAppliers()
           .For<RootEvent.Entity.NestedEntity.PropertyUpdated.Name>(e => Name = e.Name);
      }
   }
}