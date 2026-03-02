using System;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using JetBrains.Annotations;

// ReSharper disable RedundantNameQualifier

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.QueryModels;

partial class Component
{
#pragma warning disable CA1812 // Used via reflection in tuery model infrastructure
   [UsedImplicitly]public class Entity : Component.RemovableNestedEntity<Entity, Guid, ICompositeTaggregateTevent.Component.Entity, ICompositeTaggregateTevent.Component.Entity.Created, ICompositeTaggregateTevent.Component.Entity.Removed, CompositeTaggregateTevent.Component.Entity.IdGetterSetter>
#pragma warning restore CA1812
   {
      public string Name { get; private set; } = string.Empty;
      public Entity(Component parent) : base(parent)
      {
         RegisterTeventAppliers()
           .For<ICompositeTaggregateTevent.Component.Entity.PropertyUpdated.Name>(e => Name = e.Name);
      }
   }
}