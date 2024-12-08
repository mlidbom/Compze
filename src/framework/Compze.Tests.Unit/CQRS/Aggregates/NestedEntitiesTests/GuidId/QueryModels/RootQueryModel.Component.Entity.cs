using System;
using Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.Domain.Events;
using JetBrains.Annotations;

namespace Compze.Tests.Unit.CQRS.Aggregates.NestedEntitiesTests.GuidId.QueryModels;

partial class Component
{
   [UsedImplicitly]public class Entity : Component.RemovableNestedEntity<Entity, Guid, CompositeAggregateEvent.Component.Entity.IRoot, CompositeAggregateEvent.Component.Entity.Created, CompositeAggregateEvent.Component.Entity.Removed, CompositeAggregateEvent.Component.Entity.Implementation.Root.IdGetterSetter>
   {
      public string Name { get; private set; } = string.Empty;
      public Entity(Component parent) : base(parent)
      {
         RegisterEventAppliers()
           .For<CompositeAggregateEvent.Component.Entity.PropertyUpdated.Name>(e => Name = e.Name);
      }
   }
}