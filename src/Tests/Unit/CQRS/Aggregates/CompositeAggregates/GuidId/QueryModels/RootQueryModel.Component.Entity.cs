using System;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using JetBrains.Annotations;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.QueryModels;

partial class Component
{
#pragma warning disable CA1812 // Used via reflection in tuery model infrastructure
   [UsedImplicitly]public class Entity : Component.RemovableNestedEntity<Entity, Guid, CompositeTaggregateTevent.Component.Entity.IRoot, CompositeTaggregateTevent.Component.Entity.Created, CompositeTaggregateTevent.Component.Entity.Removed, CompositeTaggregateTevent.Component.Entity.Implementation.Root.IdGetterSetter>
#pragma warning restore CA1812
   {
      public string Name { get; private set; } = string.Empty;
      public Entity(Component parent) : base(parent)
      {
         RegisterTeventAppliers()
           .For<CompositeTaggregateTevent.Component.Entity.PropertyUpdated.Name>(e => Name = e.Name);
      }
   }
}