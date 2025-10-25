using System;
using Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.Domain.Tevents;
using JetBrains.Annotations;

namespace Compze.Tests.Unit.CQRS.Aggregates.CompositeAggregates.GuidId.QueryModels;

partial class Component
{
#pragma warning disable CA1812 // Used via reflection in tuery model infrastructure
   [UsedImplicitly]public class Entity : Component.RemovableNestedEntity<Entity, Guid, CompositeAggregateTevent.Component.Entity.IRoot, CompositeAggregateTevent.Component.Entity.Created, CompositeAggregateTevent.Component.Entity.Removed, CompositeAggregateTevent.Component.Entity.Implementation.Root.IdGetterSetter>
#pragma warning restore CA1812
   {
      public string Name { get; private set; } = string.Empty;
      public Entity(Component parent) : base(parent)
      {
         RegisterTeventAppliers()
           .For<CompositeAggregateTevent.Component.Entity.PropertyUpdated.Name>(e => Name = e.Name);
      }
   }
}