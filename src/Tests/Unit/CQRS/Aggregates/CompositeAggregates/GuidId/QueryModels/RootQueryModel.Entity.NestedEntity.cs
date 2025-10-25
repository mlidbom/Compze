using System;
using Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.Domain.Tevents;
using JetBrains.Annotations;

namespace Compze.Tests.Unit.CQRS.Taggregates.CompositeTaggregates.GuidId.QueryModels;

partial class Entity
{
#pragma warning disable CA1812 // Used via reflection in tuery model infrastructure
   [UsedImplicitly]public class RemovableNestedEntity : RemovableNestedEntity<RemovableNestedEntity, Guid, CompositeTaggregateTevent.Entity.NestedEntity.IRoot, CompositeTaggregateTevent.Entity.NestedEntity.Created, CompositeTaggregateTevent.Entity.NestedEntity.Removed, CompositeTaggregateTevent.Entity.NestedEntity.Implementation.Root.IdGetterSetter>
#pragma warning restore CA1812
   {
      public string Name { get; private set; } = string.Empty;
      public RemovableNestedEntity(Entity entity) : base(entity)
      {
         RegisterTeventAppliers()
           .For<CompositeTaggregateTevent.Entity.NestedEntity.PropertyUpdated.Name>(e => Name = e.Name);
      }
   }
}