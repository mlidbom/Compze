using System;
using Compze.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain.Events;
using JetBrains.Annotations;

namespace Compze.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain;

[UsedImplicitly] partial class RemovableEntity
{
   public class RemovableNestedEntity : RemovableNestedEntity<RemovableNestedEntity, Guid, RootEvent.Entity.NestedEntity.Implementation.Root, RootEvent.Entity.NestedEntity.IRoot, RootEvent.Entity.NestedEntity.Created, RootEvent.Entity.NestedEntity.Removed, RootEvent.Entity.NestedEntity.Implementation.Root.IdGetterSetter>
   {
      public string Name { get; private set; } = string.Empty;

      public RemovableNestedEntity(RemovableEntity removableEntity) : base(removableEntity)
      {
         RegisterEventAppliers()
           .For<RootEvent.Entity.NestedEntity.PropertyUpdated.Name>(e => Name = e.Name);
      }

      public void Rename(string name) => Publish(new RootEvent.Entity.NestedEntity.Implementation.Renamed(name: name));
      public void Remove() => Publish(new RootEvent.Entity.NestedEntity.Implementation.Removed());
   }
}
