using System;
using Compze.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain.Events;
using JetBrains.Annotations;

namespace Compze.Tests.CQRS.Aggregates.NestedEntitiesTests.GuidId.QueryModels;

partial class Component
{
   [UsedImplicitly]public class Entity : Component.RemovableNestedEntity<Entity, Guid, RootEvent.Component.Entity.IRoot, RootEvent.Component.Entity.Created, RootEvent.Component.Entity.Removed, RootEvent.Component.Entity.Implementation.Root.IdGetterSetter>
   {
      public string Name { get; private set; } = string.Empty;
      public Entity(Component parent) : base(parent)
      {
         RegisterEventAppliers()
           .For<RootEvent.Component.Entity.PropertyUpdated.Name>(e => Name = e.Name);
      }
   }
}